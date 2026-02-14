using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CMMS.Core.Configuration;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Core.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMMS.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUpdateService _updateService;
    private readonly LicensingSettings _settings;
    private readonly ILogger<LicenseService> _logger;

    private LicenseInfo? _cachedLicense;
    private LicenseTier _currentTier = LicenseTier.Basic;
    private LicenseStatus _currentStatus = LicenseStatus.NotActivated;
    private HashSet<string> _enabledFeatures = new();
    private bool _initialized;

    public LicenseService(
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        IUpdateService updateService,
        IOptions<LicensingSettings> settings,
        ILogger<LicenseService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _updateService = updateService;
        _settings = settings.Value;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        try
        {
            var license = await _unitOfWork.LicenseInfos.Query()
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (license != null)
            {
                _cachedLicense = license;
                UpdateInMemoryState(license);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize license state from cache");
        }

        _initialized = true;
    }

    public async Task<LicenseStatusInfo> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var license = await _unitOfWork.LicenseInfos.Query()
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (license == null)
        {
            return new LicenseStatusInfo { Status = LicenseStatus.NotActivated };
        }

        var status = DetermineStatus(license);
        var tier = Enum.TryParse<LicenseTier>(license.Tier, out var t) ? t : LicenseTier.Basic;
        var features = license.Features.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var daysUntilExpiry = (int)(license.ExpiresAt - DateTime.UtcNow).TotalDays;

        int? graceDaysRemaining = null;
        if (status == LicenseStatus.GracePeriod && license.LastPhoneHome.HasValue)
        {
            var graceEnd = license.LastPhoneHome.Value.AddDays(_settings.GracePeriodDays);
            graceDaysRemaining = Math.Max(0, (int)(graceEnd - DateTime.UtcNow).TotalDays);
        }

        return new LicenseStatusInfo
        {
            Status = status,
            Tier = tier,
            EnabledFeatures = features,
            ExpiresAt = license.ExpiresAt,
            LastPhoneHome = license.LastPhoneHome,
            DaysUntilExpiry = daysUntilExpiry,
            GraceDaysRemaining = graceDaysRemaining,
            WarningMessage = license.WarningMessage,
            IsActivated = true,
            HardwareId = license.HardwareId,
        };
    }

    public async Task<LicenseActivationResult> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var hardwareId = GenerateHardwareId();
            var machineName = Environment.MachineName;
            var osInfo = RuntimeInformation.OSDescription;

            var client = _httpClientFactory.CreateClient("LicenseServer");
            var response = await client.PostAsJsonAsync("api/v1/licenses/activate", new
            {
                licenseKey,
                hardwareId,
                machineName,
                osInfo,
            }, cancellationToken);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode || !json.GetProperty("success").GetBoolean())
            {
                var error = json.TryGetProperty("error", out var e) ? e.GetString() : "Activation failed.";
                return new LicenseActivationResult { Success = false, Error = error };
            }

            var data = json.GetProperty("data");
            var tier = data.GetProperty("tier").GetString() ?? "Basic";
            var features = data.GetProperty("features").EnumerateArray().Select(f => f.GetString()!).ToArray();
            var expiresAt = data.GetProperty("expiresAt").GetDateTime();
            var activationId = data.GetProperty("activationId").GetInt32();

            // Save to local cache
            var existing = await _unitOfWork.LicenseInfos.Query().FirstOrDefaultAsync(cancellationToken);
            if (existing != null)
            {
                existing.IsDeleted = true;
                existing.DeletedAt = DateTime.UtcNow;
            }

            var licenseInfo = new LicenseInfo
            {
                LicenseKey = licenseKey,
                Tier = tier,
                Features = string.Join(",", features),
                HardwareId = hardwareId,
                ActivationId = activationId,
                ExpiresAt = expiresAt,
                LastPhoneHome = DateTime.UtcNow,
                Status = LicenseStatus.Valid.ToString(),
            };

            await _unitOfWork.LicenseInfos.AddAsync(licenseInfo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            UpdateInMemoryState(licenseInfo);

            _logger.LogInformation("License activated successfully. Tier: {Tier}, Expires: {ExpiresAt}", tier, expiresAt);

            return new LicenseActivationResult
            {
                Success = true,
                Tier = Enum.TryParse<LicenseTier>(tier, out var lt) ? lt : LicenseTier.Basic,
                Features = features,
                ExpiresAt = expiresAt,
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach licensing server for activation");
            return new LicenseActivationResult { Success = false, Error = "Cannot reach licensing server. Please check your network connection." };
        }
    }

    public async Task<bool> DeactivateAsync(CancellationToken cancellationToken = default)
    {
        var license = await _unitOfWork.LicenseInfos.Query()
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (license == null) return false;

        try
        {
            var client = _httpClientFactory.CreateClient("LicenseServer");
            var response = await client.PostAsJsonAsync("api/v1/licenses/deactivate", new
            {
                licenseKey = license.LicenseKey,
                hardwareId = license.HardwareId,
            }, cancellationToken);

            // Even if server call fails, deactivate locally
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify licensing server of deactivation");
        }

        license.IsDeleted = true;
        license.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cachedLicense = null;
        _currentStatus = LicenseStatus.NotActivated;
        _currentTier = LicenseTier.Basic;
        _enabledFeatures.Clear();

        _logger.LogInformation("License deactivated");
        return true;
    }

    public async Task<LicensePhoneHomeResult> PhoneHomeAsync(CancellationToken cancellationToken = default)
    {
        var license = await _unitOfWork.LicenseInfos.Query()
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (license == null)
        {
            return new LicensePhoneHomeResult { Success = false, Error = "No license activated." };
        }

        try
        {
            var client = _httpClientFactory.CreateClient("LicenseServer");
            var response = await client.PostAsJsonAsync("api/v1/licenses/phone-home", new
            {
                licenseKey = license.LicenseKey,
                hardwareId = license.HardwareId,
            }, cancellationToken);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode || !json.GetProperty("success").GetBoolean())
            {
                var error = json.TryGetProperty("error", out var e) ? e.GetString() : "Phone-home failed.";

                // If revoked, mark it
                if (error?.Contains("revoked", StringComparison.OrdinalIgnoreCase) == true)
                {
                    license.Status = LicenseStatus.Revoked.ToString();
                    license.WarningMessage = "License has been revoked.";
                    license.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    UpdateInMemoryState(license);
                }

                return new LicensePhoneHomeResult { Success = false, Error = error };
            }

            var data = json.GetProperty("data");
            var daysUntilExpiry = data.GetProperty("daysUntilExpiry").GetInt32();
            var warning = data.TryGetProperty("warning", out var w) ? w.GetString() : null;

            license.LastPhoneHome = DateTime.UtcNow;
            license.Status = LicenseStatus.Valid.ToString();
            license.WarningMessage = warning;
            license.UpdatedAt = DateTime.UtcNow;

            if (data.TryGetProperty("expiresAt", out var exp))
            {
                license.ExpiresAt = exp.GetDateTime();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            UpdateInMemoryState(license);

            _logger.LogInformation("Phone-home successful. Days until expiry: {Days}", daysUntilExpiry);

            // Check for available update from phone-home response
            UpdateInfo? availableUpdate = null;
            if (data.TryGetProperty("latestVersion", out var latestVersionEl))
            {
                var latestVersion = latestVersionEl.GetString();
                var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

                if (!string.IsNullOrEmpty(latestVersion) && IsNewerVersion(latestVersion, currentVersion))
                {
                    availableUpdate = new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = data.TryGetProperty("downloadUrl", out var dl) ? dl.GetString() ?? "" : "",
                        ReleaseNotes = data.TryGetProperty("releaseNotes", out var rn) ? rn.GetString() : null,
                        FileSizeBytes = data.TryGetProperty("fileSizeBytes", out var fs) ? fs.GetInt64() : 0,
                        Sha256Hash = data.TryGetProperty("sha256Hash", out var sh) ? sh.GetString() ?? "" : "",
                        IsRequired = data.TryGetProperty("isRequired", out var ir) && ir.GetBoolean(),
                        ReleaseId = data.TryGetProperty("releaseId", out var ri) ? ri.GetInt32() : 0,
                    };

                    _updateService.SetAvailableUpdate(availableUpdate);
                    _logger.LogInformation("Update available: {Version}", latestVersion);
                }
                else
                {
                    _updateService.SetAvailableUpdate(null);
                }
            }

            return new LicensePhoneHomeResult
            {
                Success = true,
                DaysUntilExpiry = daysUntilExpiry,
                Warning = warning,
                AvailableUpdate = availableUpdate,
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Phone-home failed - licensing server unreachable");

            // Check grace period
            if (license.LastPhoneHome.HasValue)
            {
                var graceEnd = license.LastPhoneHome.Value.AddDays(_settings.GracePeriodDays);
                if (DateTime.UtcNow < graceEnd)
                {
                    var daysRemaining = (int)(graceEnd - DateTime.UtcNow).TotalDays;
                    license.Status = LicenseStatus.GracePeriod.ToString();
                    license.WarningMessage = $"Cannot reach licensing server. Grace period: {daysRemaining} days remaining.";
                    license.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    UpdateInMemoryState(license);

                    return new LicensePhoneHomeResult
                    {
                        Success = true,
                        Warning = license.WarningMessage,
                    };
                }
                else
                {
                    license.Status = LicenseStatus.Expired.ToString();
                    license.WarningMessage = "Grace period expired. Please connect to the licensing server.";
                    license.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    UpdateInMemoryState(license);
                }
            }

            return new LicensePhoneHomeResult
            {
                Success = false,
                Error = "Cannot reach licensing server and grace period has expired.",
            };
        }
    }

    public bool IsFeatureEnabled(string feature)
    {
        // When not activated, allow all features (no license enforcement until first activation)
        if (_currentStatus == LicenseStatus.NotActivated)
            return true;

        if (_currentStatus == LicenseStatus.Expired || _currentStatus == LicenseStatus.Revoked)
            return false;

        return _enabledFeatures.Contains(feature);
    }

    public LicenseTier GetCurrentTier() => _currentTier;

    public LicenseStatus GetCurrentLicenseStatus() => _currentStatus;

    private void UpdateInMemoryState(LicenseInfo license)
    {
        _cachedLicense = license;
        _currentTier = Enum.TryParse<LicenseTier>(license.Tier, out var tier) ? tier : LicenseTier.Basic;
        _currentStatus = DetermineStatus(license);
        _enabledFeatures = license.Features
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
    }

    private LicenseStatus DetermineStatus(LicenseInfo license)
    {
        if (Enum.TryParse<LicenseStatus>(license.Status, out var status))
        {
            if (status == LicenseStatus.Revoked) return LicenseStatus.Revoked;
        }

        if (license.ExpiresAt < DateTime.UtcNow)
            return LicenseStatus.Expired;

        if (license.LastPhoneHome.HasValue)
        {
            var graceEnd = license.LastPhoneHome.Value.AddDays(_settings.GracePeriodDays);
            if (DateTime.UtcNow > graceEnd)
                return LicenseStatus.Expired;

            // If last phone home is older than interval, we're in grace
            var nextPhoneHomeDue = license.LastPhoneHome.Value.AddHours(_settings.PhoneHomeIntervalHours * 2);
            if (DateTime.UtcNow > nextPhoneHomeDue)
                return LicenseStatus.GracePeriod;
        }

        return LicenseStatus.Valid;
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (Version.TryParse(latest, out var latestVer) && Version.TryParse(current, out var currentVer))
            return latestVer > currentVer;
        return false;
    }

    private static string GenerateHardwareId()
    {
        var machineName = Environment.MachineName;
        var osVersion = Environment.OSVersion.ToString();

        var macAddress = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault(addr => !string.IsNullOrEmpty(addr)) ?? "NOMAC";

        var raw = $"{machineName}|{macAddress}|{osVersion}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
