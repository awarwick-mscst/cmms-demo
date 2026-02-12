using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Services;

public class LicenseValidationService
{
    private readonly LicensingDbContext _context;
    private readonly LicenseKeyGenerator _keyGenerator;
    private readonly ILogger<LicenseValidationService> _logger;

    public LicenseValidationService(
        LicensingDbContext context,
        LicenseKeyGenerator keyGenerator,
        ILogger<LicenseValidationService> logger)
    {
        _context = context;
        _keyGenerator = keyGenerator;
        _logger = logger;
    }

    public async Task<ActivationResult> ActivateAsync(string licenseKey, string hardwareId, string machineName, string? osInfo, string? ipAddress)
    {
        var license = await _context.Licenses
            .Include(l => l.Activations.Where(a => a.IsActive))
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);

        if (license == null)
            return ActivationResult.Fail("Invalid license key.");

        if (license.IsRevoked)
            return ActivationResult.Fail("License has been revoked.");

        if (license.ExpiresAt < DateTime.UtcNow)
            return ActivationResult.Fail("License has expired.");

        if (!license.Customer.IsActive)
            return ActivationResult.Fail("Customer account is inactive.");

        // Check if already activated on this hardware
        var existing = license.Activations.FirstOrDefault(a => a.HardwareId == hardwareId);
        if (existing != null)
        {
            existing.LastPhoneHome = DateTime.UtcNow;
            existing.LastIpAddress = ipAddress;
            existing.MachineName = machineName;

            await LogAudit(license.Id, "ReActivation", $"Re-activated on {machineName}", ipAddress, hardwareId);
            await _context.SaveChangesAsync();

            return ActivationResult.Ok(license, existing);
        }

        // Check activation limit
        if (license.Activations.Count >= license.MaxActivations)
            return ActivationResult.Fail($"Maximum activations ({license.MaxActivations}) reached.");

        var activation = new LicenseActivation
        {
            LicenseId = license.Id,
            HardwareId = hardwareId,
            MachineName = machineName,
            OsInfo = osInfo,
            LastPhoneHome = DateTime.UtcNow,
            LastIpAddress = ipAddress,
        };

        _context.Activations.Add(activation);
        await LogAudit(license.Id, "Activation", $"Activated on {machineName}", ipAddress, hardwareId);
        await _context.SaveChangesAsync();

        _logger.LogInformation("License {LicenseId} activated on {Machine} ({HardwareId})", license.Id, machineName, hardwareId);

        return ActivationResult.Ok(license, activation);
    }

    public async Task<bool> DeactivateAsync(string licenseKey, string hardwareId, string? ipAddress)
    {
        var activation = await _context.Activations
            .Include(a => a.License)
            .FirstOrDefaultAsync(a => a.License.LicenseKey == licenseKey && a.HardwareId == hardwareId && a.IsActive);

        if (activation == null)
            return false;

        activation.IsActive = false;
        activation.DeactivatedAt = DateTime.UtcNow;

        await LogAudit(activation.LicenseId, "Deactivation", $"Deactivated from {activation.MachineName}", ipAddress, hardwareId);
        await _context.SaveChangesAsync();

        _logger.LogInformation("License {LicenseId} deactivated from {Machine}", activation.LicenseId, activation.MachineName);

        return true;
    }

    public async Task<PhoneHomeResult> PhoneHomeAsync(string licenseKey, string hardwareId, string? ipAddress)
    {
        var activation = await _context.Activations
            .Include(a => a.License)
            .FirstOrDefaultAsync(a => a.License.LicenseKey == licenseKey && a.HardwareId == hardwareId && a.IsActive);

        if (activation == null)
            return new PhoneHomeResult { Valid = false, Message = "No active activation found." };

        var license = activation.License;

        if (license.IsRevoked)
            return new PhoneHomeResult { Valid = false, Message = "License has been revoked." };

        activation.LastPhoneHome = DateTime.UtcNow;
        activation.LastIpAddress = ipAddress;
        await _context.SaveChangesAsync();

        var daysUntilExpiry = (license.ExpiresAt - DateTime.UtcNow).TotalDays;

        return new PhoneHomeResult
        {
            Valid = true,
            Tier = license.Tier,
            ExpiresAt = license.ExpiresAt,
            DaysUntilExpiry = (int)daysUntilExpiry,
            Warning = daysUntilExpiry <= 14 ? $"License expires in {(int)daysUntilExpiry} days." : null,
        };
    }

    public async Task<LicenseStatusResult?> GetStatusAsync(int licenseId)
    {
        var license = await _context.Licenses
            .Include(l => l.Activations.Where(a => a.IsActive))
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == licenseId);

        if (license == null) return null;

        return new LicenseStatusResult
        {
            LicenseId = license.Id,
            Tier = license.Tier,
            CustomerName = license.Customer.CompanyName,
            IsRevoked = license.IsRevoked,
            ExpiresAt = license.ExpiresAt,
            IsExpired = license.ExpiresAt < DateTime.UtcNow,
            ActiveActivations = license.Activations.Count,
            MaxActivations = license.MaxActivations,
        };
    }

    private async Task LogAudit(int licenseId, string action, string? details, string? ipAddress, string? hardwareId)
    {
        _context.AuditLogs.Add(new LicenseAuditLog
        {
            LicenseId = licenseId,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            HardwareId = hardwareId,
        });
        await Task.CompletedTask;
    }
}

public class ActivationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Tier { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
    public int? ActivationId { get; set; }

    public static ActivationResult Ok(License license, LicenseActivation activation)
    {
        return new ActivationResult
        {
            Success = true,
            Tier = license.Tier,
            Features = GetFeaturesForTier(license.Tier),
            ExpiresAt = license.ExpiresAt,
            ActivationId = activation.Id,
        };
    }

    public static ActivationResult Fail(string error) => new() { Success = false, Error = error };

    public static string[] GetFeaturesForTier(string tier) => tier switch
    {
        "Enterprise" => new[] { "work-orders", "assets", "inventory", "preventive-maintenance", "advanced-reporting", "label-printing", "ldap", "email-calendar", "backup", "api-access" },
        "Pro" => new[] { "work-orders", "assets", "inventory", "preventive-maintenance", "advanced-reporting", "label-printing" },
        "Basic" => new[] { "work-orders", "assets" },
        _ => new[] { "work-orders", "assets" },
    };
}

public class PhoneHomeResult
{
    public bool Valid { get; set; }
    public string? Message { get; set; }
    public string? Tier { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string? Warning { get; set; }
}

public class LicenseStatusResult
{
    public int LicenseId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public int ActiveActivations { get; set; }
    public int MaxActivations { get; set; }
}
