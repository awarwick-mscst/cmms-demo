namespace CMMS.Core.Entities;

/// <summary>
/// Local cache of license state for offline verification and grace period support.
/// </summary>
public class LicenseInfo : BaseEntity
{
    /// <summary>The license key string (RSA-signed token)</summary>
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>License tier: Basic, Pro, Enterprise</summary>
    public string Tier { get; set; } = "Basic";

    /// <summary>Comma-separated list of enabled features</summary>
    public string Features { get; set; } = string.Empty;

    /// <summary>Hardware fingerprint for this activation</summary>
    public string HardwareId { get; set; } = string.Empty;

    /// <summary>Activation ID from the licensing server</summary>
    public int? ActivationId { get; set; }

    /// <summary>When the license expires</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Last successful phone-home timestamp</summary>
    public DateTime? LastPhoneHome { get; set; }

    /// <summary>Last response from phone-home (encrypted JSON)</summary>
    public string? LastPhoneHomeResponse { get; set; }

    /// <summary>Current license status</summary>
    public string Status { get; set; } = "NotActivated";

    /// <summary>Warning message from server (e.g., expiring soon)</summary>
    public string? WarningMessage { get; set; }
}
