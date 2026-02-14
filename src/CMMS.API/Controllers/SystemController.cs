using System.Reflection;
using CMMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/system")]
public class SystemController : ControllerBase
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(IUpdateService updateService, ILogger<SystemController> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    [HttpGet("version")]
    [AllowAnonymous]
    public IActionResult GetVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        return Ok(new { version });
    }

    [HttpGet("update-status")]
    [Authorize]
    public IActionResult GetUpdateStatus()
    {
        var update = _updateService.GetAvailableUpdate();
        if (update == null)
            return Ok(new { updateAvailable = false });

        return Ok(new
        {
            updateAvailable = true,
            version = update.Version,
            releaseNotes = update.ReleaseNotes,
            fileSizeBytes = update.FileSizeBytes,
            isRequired = update.IsRequired,
        });
    }

    [HttpPost("update/apply")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ApplyUpdate(CancellationToken ct)
    {
        var update = _updateService.GetAvailableUpdate();
        if (update == null)
            return BadRequest(new { error = "No update available." });

        try
        {
            _logger.LogInformation("Starting update download for version {Version}", update.Version);
            await _updateService.DownloadUpdateAsync(update, ct);
            _logger.LogInformation("Update downloaded, launching updater");

            var installDir = AppContext.BaseDirectory;
            var zipPath = Path.Combine(installDir, "updates", $"cmms-{update.Version}.zip");
            _updateService.LaunchUpdater(zipPath);

            return Ok(new { message = "Update is being applied. The service will restart shortly." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply update");
            return StatusCode(500, new { error = "Failed to apply update: " + ex.Message });
        }
    }
}
