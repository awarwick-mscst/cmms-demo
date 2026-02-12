namespace LicensingServer.Web.Models;

public class LicenseActivation
{
    public int Id { get; set; }
    public int LicenseId { get; set; }
    public string HardwareId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? OsInfo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAt { get; set; }
    public DateTime? LastPhoneHome { get; set; }
    public string? LastIpAddress { get; set; }

    public virtual License License { get; set; } = null!;
}
