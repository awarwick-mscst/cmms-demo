namespace LicensingServer.Web.Models;

public class License
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string Tier { get; set; } = "Basic";
    public int MaxActivations { get; set; } = 1;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<LicenseActivation> Activations { get; set; } = new List<LicenseActivation>();
    public virtual ICollection<LicenseAuditLog> AuditLogs { get; set; } = new List<LicenseAuditLog>();
}
