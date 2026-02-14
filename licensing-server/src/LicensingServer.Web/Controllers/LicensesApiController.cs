using LicensingServer.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicensingServer.Web.Controllers;

[ApiController]
[Route("api/v1/licenses")]
public class LicensesApiController : ControllerBase
{
    private readonly LicenseValidationService _validationService;
    private readonly ILogger<LicensesApiController> _logger;

    public LicensesApiController(
        LicenseValidationService validationService,
        ILogger<LicensesApiController> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _validationService.ActivateAsync(
            request.LicenseKey, request.HardwareId, request.MachineName, request.OsInfo, ipAddress);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new
        {
            success = true,
            data = new
            {
                tier = result.Tier,
                features = result.Features,
                expiresAt = result.ExpiresAt,
                activationId = result.ActivationId,
            }
        });
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var success = await _validationService.DeactivateAsync(request.LicenseKey, request.HardwareId, ipAddress);

        if (!success)
            return NotFound(new { success = false, error = "No active activation found for this key and hardware." });

        return Ok(new { success = true, message = "License deactivated successfully." });
    }

    [HttpPost("phone-home")]
    public async Task<IActionResult> PhoneHome([FromBody] PhoneHomeRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _validationService.PhoneHomeAsync(request.LicenseKey, request.HardwareId, ipAddress);

        if (!result.Valid)
            return BadRequest(new { success = false, error = result.Message });

        var data = new Dictionary<string, object?>
        {
            ["tier"] = result.Tier,
            ["expiresAt"] = result.ExpiresAt,
            ["daysUntilExpiry"] = result.DaysUntilExpiry,
            ["warning"] = result.Warning,
        };

        if (result.LatestRelease != null)
        {
            data["latestVersion"] = result.LatestRelease.Version;
            data["downloadUrl"] = $"/api/v1/releases/{result.LatestRelease.Id}/download?licenseKey={request.LicenseKey}&hardwareId={request.HardwareId}";
            data["releaseNotes"] = result.LatestRelease.ReleaseNotes;
            data["fileSizeBytes"] = result.LatestRelease.FileSizeBytes;
            data["sha256Hash"] = result.LatestRelease.Sha256Hash;
            data["isRequired"] = result.LatestRelease.IsRequired;
            data["releaseId"] = result.LatestRelease.Id;
        }

        return Ok(new { success = true, data });
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(int id)
    {
        var result = await _validationService.GetStatusAsync(id);

        if (result == null)
            return NotFound(new { success = false, error = "License not found." });

        return Ok(new { success = true, data = result });
    }
}

public class ActivateRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? OsInfo { get; set; }
}

public class DeactivateRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
}

public class PhoneHomeRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
}
