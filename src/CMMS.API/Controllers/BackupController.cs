using System.Text.Json;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "CanManageUsers")] // Admin only
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IBackupService backupService, ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// Creates and downloads a full database backup in JSON format
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Backup export requested");

        var result = await _backupService.CreateExportAsync(cancellationToken);

        if (!result.Success || result.Data == null)
        {
            return BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "Failed to create backup"));
        }

        return File(result.Data, "application/json", result.FileName);
    }

    /// <summary>
    /// Gets information about what will be backed up
    /// </summary>
    [HttpGet("info")]
    public async Task<ActionResult<ApiResponse<BackupInfoDto>>> GetBackupInfo(CancellationToken cancellationToken)
    {
        // Create a backup to get the metadata (we won't return the data)
        var result = await _backupService.CreateExportAsync(cancellationToken);

        if (!result.Success || result.Metadata == null)
        {
            return BadRequest(ApiResponse<BackupInfoDto>.Fail("Failed to get backup info"));
        }

        var info = new BackupInfoDto
        {
            Tables = result.Metadata.RecordCounts.Keys.ToList(),
            RecordCounts = result.Metadata.RecordCounts,
            TotalRecords = result.Metadata.RecordCounts.Values.Sum(),
            EstimatedSizeBytes = result.Data?.Length ?? 0
        };

        return Ok(ApiResponse<BackupInfoDto>.Ok(info));
    }

    /// <summary>
    /// Validates an uploaded backup file
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<BackupValidationDto>>> Validate(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<BackupValidationDto>.Fail("No file uploaded"));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var backupData = await JsonSerializer.DeserializeAsync<BackupData>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (backupData == null)
            {
                return BadRequest(ApiResponse<BackupValidationDto>.Fail("Invalid backup file format"));
            }

            var result = await _backupService.ValidateBackupAsync(backupData, cancellationToken);

            var dto = new BackupValidationDto
            {
                IsValid = result.IsValid,
                Version = result.Version,
                ExportedAt = result.ExportedAt,
                Tables = result.Tables,
                RecordCounts = result.RecordCounts,
                TotalRecords = result.RecordCounts.Values.Sum(),
                Errors = result.Errors,
                Warnings = result.Warnings
            };

            return Ok(ApiResponse<BackupValidationDto>.Ok(dto));
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse<BackupValidationDto>.Fail("Invalid JSON format"));
        }
    }

    /// <summary>
    /// Imports data from a backup file
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<BackupImportResultDto>>> Import(
        IFormFile file,
        [FromQuery] bool clearExisting = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<BackupImportResultDto>.Fail("No file uploaded"));
        }

        _logger.LogInformation("Backup import requested, clearExisting: {ClearExisting}", clearExisting);

        try
        {
            using var stream = file.OpenReadStream();
            var backupData = await JsonSerializer.DeserializeAsync<BackupData>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (backupData == null)
            {
                return BadRequest(ApiResponse<BackupImportResultDto>.Fail("Invalid backup file format"));
            }

            var result = await _backupService.ImportAsync(backupData, clearExisting, cancellationToken);

            var dto = new BackupImportResultDto
            {
                Success = result.Success,
                TablesImported = result.TablesImported,
                RecordsImported = result.RecordsImported,
                Errors = result.Errors,
                Warnings = result.Warnings
            };

            if (!result.Success)
            {
                return BadRequest(ApiResponse<BackupImportResultDto>.Fail(
                    result.Errors.FirstOrDefault() ?? "Import failed"));
            }

            return Ok(ApiResponse<BackupImportResultDto>.Ok(dto, "Backup imported successfully"));
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse<BackupImportResultDto>.Fail("Invalid JSON format"));
        }
    }
}
