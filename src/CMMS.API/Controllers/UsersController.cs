using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;

    public UsersController(IUnitOfWork unitOfWork, IAuthService authService)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
    }

    /// <summary>
    /// Get all users with optional filtering
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanViewUsers")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDetailDto>>>> GetUsers(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!includeInactive)
        {
            baseQuery = baseQuery.Where(u => u.IsActive);
        }

        var users = await baseQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new UserDetailDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FirstName + " " + u.LastName,
                Phone = u.Phone,
                IsActive = u.IsActive,
                IsLocked = u.IsLocked,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<UserDetailDto>>.Ok(users));
    }

    /// <summary>
    /// Get a single user by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewUsers")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUser(int id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDetailDto>.Fail("User not found"));
        }

        var dto = new UserDetailDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        return Ok(ApiResponse<UserDetailDto>.Ok(dto));
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check if username already exists
        var existingUser = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (existingUser != null)
        {
            return BadRequest(ApiResponse<UserDetailDto>.Fail("Username already exists"));
        }

        // Check if email already exists
        existingUser = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            return BadRequest(ApiResponse<UserDetailDto>.Fail("Email already exists"));
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            PasswordChangedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Assign roles
        if (request.RoleIds?.Any() == true)
        {
            foreach (var roleId in request.RoleIds)
            {
                await _unitOfWork.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Reload with roles
        var createdUser = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        var dto = new UserDetailDto
        {
            Id = createdUser.Id,
            Username = createdUser.Username,
            Email = createdUser.Email,
            FirstName = createdUser.FirstName,
            LastName = createdUser.LastName,
            FullName = createdUser.FullName,
            Phone = createdUser.Phone,
            IsActive = createdUser.IsActive,
            IsLocked = createdUser.IsLocked,
            CreatedAt = createdUser.CreatedAt,
            Roles = createdUser.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ApiResponse<UserDetailDto>.Ok(dto, "User created successfully"));
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateUser(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDetailDto>.Fail("User not found"));
        }

        // Check if email is being changed to one that already exists
        if (user.Email != request.Email)
        {
            var existingUser = await _unitOfWork.Users.Query()
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id, cancellationToken);

            if (existingUser != null)
            {
                return BadRequest(ApiResponse<UserDetailDto>.Fail("Email already exists"));
            }
        }

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;

        // Update password if provided
        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = _authService.HashPassword(request.Password);
            user.PasswordChangedAt = DateTime.UtcNow;
        }

        // Update roles
        if (request.RoleIds != null)
        {
            // Remove existing roles
            var existingRoles = user.UserRoles.ToList();
            foreach (var role in existingRoles)
            {
                _unitOfWork.UserRoles.Remove(role);
            }

            // Add new roles
            foreach (var roleId in request.RoleIds)
            {
                await _unitOfWork.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with roles
        var updatedUser = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        var dto = new UserDetailDto
        {
            Id = updatedUser.Id,
            Username = updatedUser.Username,
            Email = updatedUser.Email,
            FirstName = updatedUser.FirstName,
            LastName = updatedUser.LastName,
            FullName = updatedUser.FullName,
            Phone = updatedUser.Phone,
            IsActive = updatedUser.IsActive,
            IsLocked = updatedUser.IsLocked,
            LastLoginAt = updatedUser.LastLoginAt,
            CreatedAt = updatedUser.CreatedAt,
            Roles = updatedUser.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        return Ok(ApiResponse<UserDetailDto>.Ok(dto, "User updated successfully"));
    }

    /// <summary>
    /// Delete (deactivate) a user
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(int id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found"));
        }

        // Don't allow deleting the last admin
        if (user.Username == "admin")
        {
            return BadRequest(ApiResponse<object>.Fail("Cannot delete the primary admin user"));
        }

        // Soft delete - just deactivate
        user.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "User deactivated successfully"));
    }

    /// <summary>
    /// Unlock a locked user account
    /// </summary>
    [HttpPost("{id}/unlock")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult<ApiResponse<object>>> UnlockUser(int id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found"));
        }

        user.IsLocked = false;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "User account unlocked"));
    }

    /// <summary>
    /// Reset a user's password
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        int id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found"));
        }

        user.PasswordHash = _authService.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Password reset successfully"));
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = "CanViewUsers")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles(CancellationToken cancellationToken = default)
    {
        var roles = await _unitOfWork.Roles.Query()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roles));
    }

    /// <summary>
    /// Get technicians/maintenance staff for work order assignment
    /// </summary>
    [HttpGet("technicians")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserSummaryDto>>>> GetTechnicians(
        CancellationToken cancellationToken = default)
    {
        var technicianRoles = new[] { "Technician", "MaintenanceManager", "Administrator" };

        var technicians = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .Where(u => u.UserRoles.Any(ur => technicianRoles.Contains(ur.Role.Name)))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FirstName + " " + u.LastName,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<UserSummaryDto>>.Ok(technicians));
    }
}

// DTOs
public class UserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UserDetailDto : UserSummaryDto
{
    public string? Phone { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int>? RoleIds { get; set; }
}

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int>? RoleIds { get; set; }
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
