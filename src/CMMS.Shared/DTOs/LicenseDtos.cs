namespace CMMS.Shared.DTOs;

public class LicenseStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string[] EnabledFeatures { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastPhoneHome { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public int? GraceDaysRemaining { get; set; }
    public string? WarningMessage { get; set; }
    public bool IsActivated { get; set; }
}

public class ActivateLicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
}

public class ActivateLicenseResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Tier { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
}
