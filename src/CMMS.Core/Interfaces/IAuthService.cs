using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password, string? ipAddress = null);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null);
    Task<bool> RevokeAllUserTokensAsync(int userId, string? ipAddress = null);
    Task<User?> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task RecordFailedLoginAsync(User user);
    Task RecordSuccessfulLoginAsync(User user, string? ipAddress = null);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public User? User { get; set; }
    public string? Error { get; set; }
    public bool IsLockedOut { get; set; }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<int> RoleIds { get; set; } = new();
}
