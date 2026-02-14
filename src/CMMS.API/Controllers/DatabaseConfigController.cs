using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DatabaseConfigController : ControllerBase
{
    private readonly IDatabaseConfigService _configService;
    private readonly ILogger<DatabaseConfigController> _logger;

    public DatabaseConfigController(
        IDatabaseConfigService configService,
        ILogger<DatabaseConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Check if the database has been configured
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> GetStatus()
    {
        var isConfigured = await _configService.IsConfiguredAsync();
        return Ok(ApiResponse<bool>.Ok(isConfigured));
    }

    /// <summary>
    /// Get current database settings (password masked)
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<DatabaseSettingsDto>>> GetSettings()
    {
        var settings = await _configService.GetSettingsAsync();

        var dto = new DatabaseSettingsDto
        {
            Provider = settings.Provider.ToString(),
            Server = settings.Server,
            Port = settings.Port,
            Database = settings.Database,
            AuthType = settings.AuthType.ToString(),
            Username = settings.Username,
            Password = string.IsNullOrEmpty(settings.Password) ? null : "********",
            AdditionalOptions = settings.AdditionalOptions,
            FilePath = settings.FilePath,
            IsConfigured = settings.IsConfigured,
            Tier = settings.Tier.ToString()
        };

        return Ok(ApiResponse<DatabaseSettingsDto>.Ok(dto));
    }

    /// <summary>
    /// Save database settings
    /// </summary>
    [HttpPost("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> SaveSettings([FromBody] DatabaseSettingsDto dto)
    {
        try
        {
            var settings = new DatabaseSettings
            {
                Provider = Enum.Parse<DatabaseProvider>(dto.Provider, true),
                Server = dto.Server,
                Port = dto.Port,
                Database = dto.Database,
                AuthType = Enum.Parse<DatabaseAuthType>(dto.AuthType, true),
                Username = dto.Username,
                Password = dto.Password,
                AdditionalOptions = dto.AdditionalOptions,
                FilePath = dto.FilePath,
                IsConfigured = true,
                Tier = Enum.Parse<DeploymentTier>(dto.Tier, true)
            };

            await _configService.SaveSettingsAsync(settings);

            _logger.LogInformation("Database settings saved successfully");
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save database settings");
            return BadRequest(ApiResponse<bool>.Fail($"Failed to save settings: {ex.Message}"));
        }
    }

    /// <summary>
    /// Test database connection
    /// </summary>
    [HttpPost("test")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DatabaseTestResultDto>>> TestConnection([FromBody] DatabaseTestRequestDto dto)
    {
        try
        {
            var settings = new DatabaseSettings
            {
                Provider = Enum.Parse<DatabaseProvider>(dto.Provider, true),
                Server = dto.Server,
                Port = dto.Port,
                Database = dto.Database,
                AuthType = Enum.Parse<DatabaseAuthType>(dto.AuthType, true),
                Username = dto.Username,
                Password = dto.Password,
                AdditionalOptions = dto.AdditionalOptions,
                FilePath = dto.FilePath
            };

            var result = await _configService.TestConnectionAsync(settings);

            var resultDto = new DatabaseTestResultDto
            {
                Success = result.Success,
                Message = result.Message,
                ServerVersion = result.ServerVersion,
                ErrorDetails = result.ErrorDetails,
                LatencyMs = result.LatencyMs
            };

            return Ok(ApiResponse<DatabaseTestResultDto>.Ok(resultDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test database connection");
            return Ok(ApiResponse<DatabaseTestResultDto>.Ok(new DatabaseTestResultDto
            {
                Success = false,
                Message = "Connection test failed",
                ErrorDetails = ex.Message
            }));
        }
    }

    /// <summary>
    /// Get available database providers
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<List<DatabaseProviderInfoDto>>> GetProviders()
    {
        var providers = new List<DatabaseProviderInfoDto>
        {
            new()
            {
                Name = "SqlServer",
                DisplayName = "Microsoft SQL Server",
                DefaultPort = 1433,
                SupportsWindowsAuth = true,
                RequiresFilePath = false,
                IsSupported = true
            },
            new()
            {
                Name = "Sqlite",
                DisplayName = "SQLite",
                DefaultPort = 0,
                SupportsWindowsAuth = false,
                RequiresFilePath = true,
                IsSupported = true
            },
            new()
            {
                Name = "PostgreSql",
                DisplayName = "PostgreSQL",
                DefaultPort = 5432,
                SupportsWindowsAuth = false,
                RequiresFilePath = false,
                IsSupported = true
            },
            new()
            {
                Name = "MySql",
                DisplayName = "MySQL / MariaDB",
                DefaultPort = 3306,
                SupportsWindowsAuth = false,
                RequiresFilePath = false,
                IsSupported = false,
                NotSupportedReason = "MySQL support is planned for a future release"
            }
        };

        return Ok(ApiResponse<List<DatabaseProviderInfoDto>>.Ok(providers));
    }

    /// <summary>
    /// Initial setup - save settings without requiring auth
    /// </summary>
    [HttpPost("setup")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> InitialSetup([FromBody] DatabaseSettingsDto dto)
    {
        // Only allow if not already configured
        var isConfigured = await _configService.IsConfiguredAsync();
        if (isConfigured)
        {
            return BadRequest(ApiResponse<bool>.Fail("Database is already configured. Use the settings endpoint instead."));
        }

        try
        {
            var settings = new DatabaseSettings
            {
                Provider = Enum.Parse<DatabaseProvider>(dto.Provider, true),
                Server = dto.Server,
                Port = dto.Port,
                Database = dto.Database,
                AuthType = Enum.Parse<DatabaseAuthType>(dto.AuthType, true),
                Username = dto.Username,
                Password = dto.Password,
                AdditionalOptions = dto.AdditionalOptions,
                FilePath = dto.FilePath,
                IsConfigured = true,
                Tier = Enum.Parse<DeploymentTier>(dto.Tier, true)
            };

            await _configService.SaveSettingsAsync(settings);

            _logger.LogInformation("Initial database setup completed");
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete initial setup");
            return BadRequest(ApiResponse<bool>.Fail($"Setup failed: {ex.Message}"));
        }
    }
}
