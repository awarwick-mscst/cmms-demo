using CMMS.Core.Enums;

namespace CMMS.Core.Interfaces;

public interface ILicenseService
{
    Task<LicenseStatusInfo> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
    Task<LicenseActivationResult> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(CancellationToken cancellationToken = default);
    Task<LicensePhoneHomeResult> PhoneHomeAsync(CancellationToken cancellationToken = default);
    bool IsFeatureEnabled(string feature);
    LicenseTier GetCurrentTier();
    LicenseStatus GetCurrentLicenseStatus();
}

public class LicenseStatusInfo
{
    public LicenseStatus Status { get; set; } = LicenseStatus.NotActivated;
    public LicenseTier Tier { get; set; } = LicenseTier.Basic;
    public string[] EnabledFeatures { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastPhoneHome { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public int? GraceDaysRemaining { get; set; }
    public string? WarningMessage { get; set; }
    public bool IsActivated { get; set; }
    public string? HardwareId { get; set; }
}

public class LicenseActivationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public LicenseTier? Tier { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
}

public class LicensePhoneHomeResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Warning { get; set; }
    public int? DaysUntilExpiry { get; set; }
}
