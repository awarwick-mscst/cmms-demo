namespace LicensingServer.Web.Models;

public class AdminLoginAuditLog
{
    public int Id { get; set; }
    public int? AdminUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string AuthMethod { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public virtual AdminUser? AdminUser { get; set; }
}
