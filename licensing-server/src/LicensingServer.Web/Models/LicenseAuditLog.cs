namespace LicensingServer.Web.Models;

public class LicenseAuditLog
{
    public int Id { get; set; }
    public int LicenseId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? HardwareId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public virtual License License { get; set; } = null!;
}
