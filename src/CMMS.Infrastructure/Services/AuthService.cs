using System.Security.Cryptography;
using System.Text;
using CMMS.Core.Configuration;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ILdapService _ldapService;
    private readonly LdapSettings _ldapSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly int _maxFailedAttempts;
    private readonly int _lockoutMinutes;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ILdapService ldapService,
        IOptions<LdapSettings> ldapSettings,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _ldapService = ldapService;
        _ldapSettings = ldapSettings.Value;
        _logger = logger;
        _maxFailedAttempts = int.Parse(configuration["Security:MaxFailedLoginAttempts"] ?? "5");
        _lockoutMinutes = int.Parse(configuration["Security:LockoutMinutes"] ?? "15");
    }

    public async Task<AuthResult> LoginAsync(string username, string password, string? ipAddress = null)
    {
        // Determine effective authentication mode
        var authMode = _ldapSettings.Enabled ? _ldapSettings.AuthenticationMode : AuthenticationMode.LocalOnly;

        // For LdapOnly mode, always try LDAP first (user may not exist locally yet)
        if (authMode == AuthenticationMode.LdapOnly)
        {
            return await AuthenticateLdapOnlyAsync(username, password, ipAddress);
        }

        // Look up existing user
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

        // For Mixed mode with no existing user, try LDAP to create one
        if (user == null && authMode == AuthenticationMode.Mixed && _ldapService.IsEnabled)
        {
            return await AuthenticateAndCreateLdapUserAsync(username, password, ipAddress);
        }

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

        // Route to appropriate authentication method based on user's type and system mode
        return await AuthenticateUserAsync(user, password, ipAddress, authMode);
    }

    private async Task<AuthResult> AuthenticateUserAsync(User user, string password, string? ipAddress, AuthenticationMode authMode)
    {
        // Local-only mode or local-only user
        if (authMode == AuthenticationMode.LocalOnly || user.AuthenticationType == AuthenticationType.Local)
        {
            return await AuthenticateLocalAsync(user, password, ipAddress);
        }

        // LDAP-only user
        if (user.AuthenticationType == AuthenticationType.Ldap)
        {
            return await AuthenticateLdapUserAsync(user, password, ipAddress);
        }

        // Mixed/Both authentication - try LDAP first, then local
        if (user.AuthenticationType == AuthenticationType.Both)
        {
            var ldapResult = await AuthenticateLdapUserAsync(user, password, ipAddress);
            if (ldapResult.Success)
            {
                return ldapResult;
            }

            // Fall back to local if allowed
            if (_ldapSettings.AllowLocalFallback)
            {
                _logger.LogInformation("LDAP auth failed for user {Username}, falling back to local", user.Username);
                return await AuthenticateLocalAsync(user, password, ipAddress);
            }

            return ldapResult;
        }

        // Default to local authentication
        return await AuthenticateLocalAsync(user, password, ipAddress);
    }

    private async Task<AuthResult> AuthenticateLocalAsync(User user, string password, string? ipAddress)
    {
        if (!VerifyPassword(password, user.PasswordHash))
        {
            await RecordFailedLoginAsync(user);
            return new AuthResult { Success = false, Error = "Invalid username or password" };
        }

        await RecordSuccessfulLoginAsync(user, ipAddress);
        return await GenerateTokensForUserAsync(user, ipAddress);
    }

    private async Task<AuthResult> AuthenticateLdapUserAsync(User user, string password, string? ipAddress)
    {
        var ldapResult = await _ldapService.AuthenticateAsync(user.Username, password);
        if (!ldapResult.Success)
        {
            await RecordFailedLoginAsync(user);
            return new AuthResult { Success = false, Error = ldapResult.Error ?? "LDAP authentication failed" };
        }

        // Sync user attributes if enabled
        if (_ldapSettings.SyncUserAttributes && ldapResult.UserInfo != null)
        {
            await SyncUserFromLdapAsync(user, ldapResult.UserInfo, ldapResult.Groups);
        }

        await RecordSuccessfulLoginAsync(user, ipAddress);
        return await GenerateTokensForUserAsync(user, ipAddress);
    }

    private async Task<AuthResult> AuthenticateLdapOnlyAsync(string username, string password, string? ipAddress)
    {
        var ldapResult = await _ldapService.AuthenticateAsync(username, password);
        if (!ldapResult.Success)
        {
            _logger.LogWarning("LDAP-only authentication failed for {Username}: {Error}", username, ldapResult.Error);
            return new AuthResult { Success = false, Error = ldapResult.Error ?? "LDAP authentication failed" };
        }

        // Find or create user
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            user = await CreateUserFromLdapAsync(ldapResult.UserInfo!, ldapResult.Groups);
        }
        else
        {
            if (_ldapSettings.SyncUserAttributes)
            {
                await SyncUserFromLdapAsync(user, ldapResult.UserInfo!, ldapResult.Groups);
            }
        }

        if (!user.IsActive)
        {
            return new AuthResult { Success = false, Error = "Account is disabled" };
        }

        await RecordSuccessfulLoginAsync(user, ipAddress);
        return await GenerateTokensForUserAsync(user, ipAddress);
    }

    private async Task<AuthResult> AuthenticateAndCreateLdapUserAsync(string username, string password, string? ipAddress)
    {
        var ldapResult = await _ldapService.AuthenticateAsync(username, password);
        if (!ldapResult.Success)
        {
            // LDAP failed and no local user exists
            return new AuthResult { Success = false, Error = "Invalid username or password" };
        }

        // Create new user from LDAP
        var user = await CreateUserFromLdapAsync(ldapResult.UserInfo!, ldapResult.Groups);
        await RecordSuccessfulLoginAsync(user, ipAddress);
        return await GenerateTokensForUserAsync(user, ipAddress);
    }

    private async Task<AuthResult> GenerateTokensForUserAsync(User user, string? ipAddress)
    {
        // Reload user with roles if needed
        if (!user.UserRoles.Any())
        {
            user = await _unitOfWork.Users.Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstAsync(u => u.Id == user.Id);
        }

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

    private async Task<User> CreateUserFromLdapAsync(LdapUserInfo ldapInfo, IReadOnlyList<string> ldapGroups)
    {
        _logger.LogInformation("Creating new user from LDAP: {Username}", ldapInfo.Username);

        var user = new User
        {
            Username = ldapInfo.Username,
            Email = !string.IsNullOrEmpty(ldapInfo.Email) ? ldapInfo.Email : $"{ldapInfo.Username}@ldap.local",
            PasswordHash = string.Empty, // LDAP users don't have local passwords
            FirstName = !string.IsNullOrEmpty(ldapInfo.FirstName) ? ldapInfo.FirstName : ldapInfo.Username,
            LastName = !string.IsNullOrEmpty(ldapInfo.LastName) ? ldapInfo.LastName : string.Empty,
            Phone = ldapInfo.Phone,
            IsActive = true,
            IsLdapUser = true,
            LdapDistinguishedName = ldapInfo.DistinguishedName,
            LdapLastSyncAt = DateTime.UtcNow,
            AuthenticationType = _ldapSettings.AuthenticationMode == AuthenticationMode.LdapOnly
                ? AuthenticationType.Ldap
                : AuthenticationType.Both
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Assign roles based on LDAP groups
        await AssignRolesFromLdapGroupsAsync(user, ldapGroups);

        return user;
    }

    private async Task SyncUserFromLdapAsync(User user, LdapUserInfo ldapInfo, IReadOnlyList<string> ldapGroups)
    {
        _logger.LogDebug("Syncing user attributes from LDAP: {Username}", user.Username);

        // Update user attributes
        if (!string.IsNullOrEmpty(ldapInfo.Email))
        {
            user.Email = ldapInfo.Email;
        }
        if (!string.IsNullOrEmpty(ldapInfo.FirstName))
        {
            user.FirstName = ldapInfo.FirstName;
        }
        if (!string.IsNullOrEmpty(ldapInfo.LastName))
        {
            user.LastName = ldapInfo.LastName;
        }
        if (!string.IsNullOrEmpty(ldapInfo.Phone))
        {
            user.Phone = ldapInfo.Phone;
        }

        user.LdapDistinguishedName = ldapInfo.DistinguishedName;
        user.LdapLastSyncAt = DateTime.UtcNow;
        user.IsLdapUser = true;
        user.UpdatedAt = DateTime.UtcNow;

        // Update roles if group mappings are configured
        if (_ldapSettings.GroupMappings.Any())
        {
            await AssignRolesFromLdapGroupsAsync(user, ldapGroups);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task AssignRolesFromLdapGroupsAsync(User user, IReadOnlyList<string> ldapGroups)
    {
        var rolesToAssign = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Map LDAP groups to roles
        foreach (var groupDn in ldapGroups)
        {
            var mapping = _ldapSettings.GroupMappings
                .FirstOrDefault(m => string.Equals(m.LdapGroup, groupDn, StringComparison.OrdinalIgnoreCase));

            if (mapping != null)
            {
                rolesToAssign.Add(mapping.RoleName);
            }
        }

        // If no groups matched, use default roles
        if (rolesToAssign.Count == 0)
        {
            foreach (var defaultRole in _ldapSettings.DefaultRoles)
            {
                rolesToAssign.Add(defaultRole);
            }
        }

        // Get role entities
        var allRoles = await _unitOfWork.Roles.GetAllAsync();
        var rolesToAdd = allRoles.Where(r => rolesToAssign.Contains(r.Name)).ToList();

        // Remove existing roles that are no longer mapped
        var existingUserRoles = await _unitOfWork.UserRoles
            .FindAsync(ur => ur.UserId == user.Id);

        foreach (var existingRole in existingUserRoles.ToList())
        {
            _unitOfWork.UserRoles.Remove(existingRole);
        }

        // Add new roles
        foreach (var role in rolesToAdd)
        {
            await _unitOfWork.UserRoles.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Assigned roles to LDAP user {Username}: {Roles}",
            user.Username, string.Join(", ", rolesToAdd.Select(r => r.Name)));
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
