namespace LicensingServer.Web.Models;

public class AdminUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool RequireMfa { get; set; } = true;
    public bool TotpEnabled { get; set; }
    public string? TotpSecretEncrypted { get; set; }
    public string? RecoveryCodesEncrypted { get; set; }
    public bool AccountLocked { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Fido2Credential> Fido2Credentials { get; set; } = new List<Fido2Credential>();
    public virtual ICollection<AdminLoginAuditLog> AuditLogs { get; set; } = new List<AdminLoginAuditLog>();
}
