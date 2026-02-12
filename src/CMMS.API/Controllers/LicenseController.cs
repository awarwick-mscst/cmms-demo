using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/license")]
[Authorize]
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(ILicenseService licenseService, ILogger<LicenseController> logger)
    {
        _licenseService = licenseService;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        var status = await _licenseService.GetCurrentStatusAsync();
        return Ok(new
        {
            success = true,
            data = new LicenseStatusDto
            {
                Status = status.Status.ToString(),
                Tier = status.Tier.ToString(),
                EnabledFeatures = status.EnabledFeatures,
                ExpiresAt = status.ExpiresAt,
                LastPhoneHome = status.LastPhoneHome,
                DaysUntilExpiry = status.DaysUntilExpiry,
                GraceDaysRemaining = status.GraceDaysRemaining,
                WarningMessage = status.WarningMessage,
                IsActivated = status.IsActivated,
            }
        });
    }

    [HttpPost("activate")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult> Activate([FromBody] ActivateLicenseRequest request)
    {
        _logger.LogInformation("License activation requested");

        var result = await _licenseService.ActivateAsync(request.LicenseKey);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new
        {
            success = true,
            data = new ActivateLicenseResponse
            {
                Success = true,
                Tier = result.Tier?.ToString(),
                Features = result.Features,
                ExpiresAt = result.ExpiresAt,
            },
            message = "License activated successfully.",
        });
    }

    [HttpPost("deactivate")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult> Deactivate()
    {
        _logger.LogInformation("License deactivation requested");

        var success = await _licenseService.DeactivateAsync();

        if (!success)
            return BadRequest(new { success = false, error = "No active license to deactivate." });

        return Ok(new { success = true, message = "License deactivated successfully." });
    }

    [HttpPost("phone-home")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult> ForcePhoneHome()
    {
        _logger.LogInformation("Manual phone-home requested");

        var result = await _licenseService.PhoneHomeAsync();

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new
        {
            success = true,
            data = new
            {
                daysUntilExpiry = result.DaysUntilExpiry,
                warning = result.Warning,
            },
            message = "Phone-home successful.",
        });
    }
}
