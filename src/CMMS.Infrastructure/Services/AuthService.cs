using System.Security.Cryptography;
using System.Text;
using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CMMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly int _maxFailedAttempts;
    private readonly int _lockoutMinutes;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _maxFailedAttempts = int.Parse(configuration["Security:MaxFailedLoginAttempts"] ?? "5");
        _lockoutMinutes = int.Parse(configuration["Security:LockoutMinutes"] ?? "15");
    }

    public async Task<AuthResult> LoginAsync(string username, string password, string? ipAddress = null)
    {
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

        if (user == null)
        {
            return new AuthResult { Success = false, Error = "Invalid username or password" };
        }

        if (!user.IsActive)
        {
            return new AuthResult { Success = false, Error = "Account is disabled" };
        }

        if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return new AuthResult
            {
                Success = false,
                Error = $"Account is locked. Try again after {user.LockoutEnd.Value:HH:mm:ss} UTC",
                IsLockedOut = true
            };
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            await RecordFailedLoginAsync(user);
            return new AuthResult { Success = false, Error = "Invalid username or password" };
        }

        await RecordSuccessfulLoginAsync(user, ipAddress);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = user
        };
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var tokenHash = ComputeHash(refreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.Query()
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken == null)
        {
            return new AuthResult { Success = false, Error = "Invalid refresh token" };
        }

        if (!storedToken.IsActive)
        {
            return new AuthResult { Success = false, Error = "Token has been revoked or expired" };
        }

        var user = storedToken.User;
        if (!user.IsActive)
        {
            return new AuthResult { Success = false, Error = "Account is disabled" };
        }

        // Revoke old token
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        // Generate new tokens
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);
        storedToken.ReplacedByToken = newRefreshToken.Token;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = user
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var tokenHash = ComputeHash(refreshToken);
        var storedToken = await _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken == null || !storedToken.IsActive)
        {
            return false;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(int userId, string? ipAddress = null)
    {
        var tokens = await _unitOfWork.RefreshTokens
            .FindAsync(rt => rt.UserId == userId && rt.RevokedAt == null);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<User?> RegisterAsync(RegisterRequest request)
    {
        // Check if username or email already exists
        if (await _unitOfWork.Users.AnyAsync(u => u.Username == request.Username))
        {
            return null;
        }

        if (await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
        {
            return null;
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Assign roles
        if (request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                await _unitOfWork.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        return user;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (!VerifyPassword(currentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        // Revoke all refresh tokens
        await RevokeAllUserTokensAsync(userId);

        return true;
    }

    public async Task RecordFailedLoginAsync(User user)
    {
        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= _maxFailedAttempts)
        {
            user.IsLocked = true;
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(_lockoutMinutes);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RecordSuccessfulLoginAsync(User user, string? ipAddress = null)
    {
        user.FailedLoginAttempts = 0;
        user.IsLocked = false;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(11));
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
