using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Core.Licensing;

namespace CMMS.Infrastructure.Services;

/// <summary>
/// Development/debug stub that enables all features without requiring a license.
/// Used when licensing is disabled in configuration.
/// </summary>
public class UnlicensedModeService : ILicenseService
{
    public Task<LicenseStatusInfo> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LicenseStatusInfo
        {
            Status = LicenseStatus.Valid,
            Tier = LicenseTier.Enterprise,
            EnabledFeatures = FeatureGate.GetAllFeatures(),
            ExpiresAt = DateTime.UtcNow.AddYears(100),
            IsActivated = true,
        });
    }

    public Task<LicenseActivationResult> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LicenseActivationResult
        {
            Success = true,
            Tier = LicenseTier.Enterprise,
            Features = FeatureGate.GetAllFeatures(),
            ExpiresAt = DateTime.UtcNow.AddYears(100),
        });
    }

    public Task<bool> DeactivateAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<LicensePhoneHomeResult> PhoneHomeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LicensePhoneHomeResult { Success = true });
    }

    public bool IsFeatureEnabled(string feature) => true;

    public LicenseTier GetCurrentTier() => LicenseTier.Enterprise;

    public LicenseStatus GetCurrentLicenseStatus() => LicenseStatus.Valid;
}
