using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.LoginAsync(request.Username, request.Password, ipAddress);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail(result.Error ?? "Login failed"));
        }

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresAt = result.ExpiresAt!.Value,
            User = MapUserToDto(result.User!)
        };

        return Ok(ApiResponse<LoginResponse>.Ok(response));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail(result.Error ?? "Token refresh failed"));
        }

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresAt = result.ExpiresAt!.Value,
            User = MapUserToDto(result.User!)
        };

        return Ok(ApiResponse<LoginResponse>.Ok(response));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = GetIpAddress();
        var success = await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);

        if (!success)
        {
            return BadRequest(ApiResponse.Fail("Invalid token"));
        }

        return Ok(ApiResponse.Ok("Logged out successfully"));
    }

    [HttpPost("register")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterUserRequest request)
    {
        var registerRequest = new RegisterRequest
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            RoleIds = request.RoleIds
        };

        var user = await _authService.RegisterAsync(registerRequest);

        if (user == null)
        {
            return BadRequest(ApiResponse<UserDto>.Fail("Username or email already exists"));
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Phone = user.Phone,
            IsActive = user.IsActive
        };

        return CreatedAtAction(nameof(Register), ApiResponse<UserDto>.Ok(userDto, "User registered successfully"));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var success = await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);

        if (!success)
        {
            return BadRequest(ApiResponse.Fail("Current password is incorrect"));
        }

        return Ok(ApiResponse.Ok("Password changed successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<UserDto>> GetCurrentUser()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("User not authenticated"));
        }

        var userDto = new UserDto
        {
            Id = userId.Value,
            Username = _currentUserService.Username ?? string.Empty,
            Roles = _currentUserService.Roles.ToList(),
            Permissions = _currentUserService.Permissions.ToList()
        };

        return Ok(ApiResponse<UserDto>.Ok(userDto));
    }

    private UserDto MapUserToDto(Core.Entities.User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            Permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList()
        };
    }

    private string? GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }

        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
    }
}
