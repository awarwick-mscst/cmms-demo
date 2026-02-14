using LicensingServer.Web.Data;
using LicensingServer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Controllers;

[ApiController]
[Route("api/v1/releases")]
public class ReleasesApiController : ControllerBase
{
    private readonly LicensingDbContext _context;
    private readonly LicenseValidationService _validationService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReleasesApiController> _logger;

    public ReleasesApiController(
        LicensingDbContext context,
        LicenseValidationService validationService,
        IWebHostEnvironment env,
        ILogger<ReleasesApiController> logger)
    {
        _context = context;
        _validationService = validationService;
        _env = env;
        _logger = logger;
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id, [FromQuery] string licenseKey, [FromQuery] string hardwareId)
    {
        if (string.IsNullOrEmpty(licenseKey) || string.IsNullOrEmpty(hardwareId))
            return BadRequest(new { error = "licenseKey and hardwareId are required." });

        // Validate the license before allowing download
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var phoneHome = await _validationService.PhoneHomeAsync(licenseKey, hardwareId, ipAddress);
        if (!phoneHome.Valid)
            return Unauthorized(new { error = "Invalid or inactive license." });

        var release = await _context.Releases.FindAsync(id);
        if (release == null || !release.IsActive)
            return NotFound(new { error = "Release not found." });

        var releasesDir = Path.Combine(_env.ContentRootPath, "releases");
        var filePath = Path.Combine(releasesDir, release.FileName);

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("Release file not found on disk: {Path}", filePath);
            return NotFound(new { error = "Release file not available." });
        }

        _logger.LogInformation("License {LicenseKey} downloading release {Version}", licenseKey, release.Version);

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "application/zip", release.FileName);
    }
}
