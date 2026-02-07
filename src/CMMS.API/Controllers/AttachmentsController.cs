using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;

    public AttachmentsController(
        IAttachmentService attachmentService,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment)
    {
        _attachmentService = attachmentService;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AttachmentDto>>>> GetAttachments(
        [FromQuery] string entityType,
        [FromQuery] int entityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            return BadRequest(ApiResponse<IEnumerable<AttachmentDto>>.Fail("Entity type and ID are required"));

        var attachments = await _attachmentService.GetAttachmentsAsync(entityType, entityId, cancellationToken);
        var dtos = attachments.Select(MapToDto);

        return Ok(ApiResponse<IEnumerable<AttachmentDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> GetAttachment(int id, CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentService.GetAttachmentByIdAsync(id, cancellationToken);
        if (attachment == null)
            return NotFound(ApiResponse<AttachmentDto>.Fail("Attachment not found"));

        return Ok(ApiResponse<AttachmentDto>.Ok(MapToDto(attachment)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IEnumerable<AttachmentDto>>>> UploadAttachments(
        [FromForm] string entityType,
        [FromForm] int entityId,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<IEnumerable<AttachmentDto>>.Fail("User not authenticated"));

        if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            return BadRequest(ApiResponse<IEnumerable<AttachmentDto>>.Fail("Entity type and ID are required"));

        if (files == null || files.Count == 0)
            return BadRequest(ApiResponse<IEnumerable<AttachmentDto>>.Fail("At least one file is required"));

        var uploadedAttachments = new List<AttachmentDto>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            // Validate file
            if (!_fileStorageService.ValidateFile(file.FileName, file.Length, out var validationError))
            {
                errors.Add($"{file.FileName}: {validationError}");
                continue;
            }

            // Save file
            using var stream = file.OpenReadStream();
            var uploadResult = await _fileStorageService.SaveFileAsync(stream, file.FileName, entityType, entityId, cancellationToken);

            if (!uploadResult.Success)
            {
                errors.Add($"{file.FileName}: {uploadResult.ErrorMessage}");
                continue;
            }

            // Get next display order
            var displayOrder = await _attachmentService.GetNextDisplayOrderAsync(entityType, entityId, cancellationToken);

            // Create attachment record
            var attachment = new Attachment
            {
                EntityType = entityType,
                EntityId = entityId,
                AttachmentType = uploadResult.AttachmentType!,
                Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(file.FileName) : title,
                FileName = uploadResult.FileName!,
                FilePath = uploadResult.FilePath!,
                FileSize = uploadResult.FileSize,
                MimeType = uploadResult.MimeType!,
                Description = description,
                DisplayOrder = displayOrder
            };

            await _attachmentService.CreateAttachmentAsync(attachment, userId.Value, cancellationToken);

            // Reload to get navigation properties
            var created = await _attachmentService.GetAttachmentByIdAsync(attachment.Id, cancellationToken);
            if (created != null)
            {
                uploadedAttachments.Add(MapToDto(created));
            }
        }

        if (errors.Count > 0 && uploadedAttachments.Count == 0)
            return BadRequest(ApiResponse<IEnumerable<AttachmentDto>>.Fail(errors));

        var response = ApiResponse<IEnumerable<AttachmentDto>>.Ok(uploadedAttachments,
            errors.Count > 0 ? $"Some files failed to upload: {string.Join("; ", errors)}" : "Files uploaded successfully");

        return CreatedAtAction(nameof(GetAttachments), new { entityType, entityId }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> UpdateAttachment(
        int id,
        [FromBody] UpdateAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<AttachmentDto>.Fail("User not authenticated"));

        var attachment = await _attachmentService.GetAttachmentByIdAsync(id, cancellationToken);
        if (attachment == null)
            return NotFound(ApiResponse<AttachmentDto>.Fail("Attachment not found"));

        if (!string.IsNullOrWhiteSpace(request.Title))
            attachment.Title = request.Title;

        if (request.Description != null)
            attachment.Description = request.Description;

        if (request.DisplayOrder.HasValue)
            attachment.DisplayOrder = request.DisplayOrder.Value;

        await _attachmentService.UpdateAttachmentAsync(attachment, userId.Value, cancellationToken);

        var updated = await _attachmentService.GetAttachmentByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<AttachmentDto>.Ok(MapToDto(updated!), "Attachment updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteAttachment(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Fail("User not authenticated"));

        var attachment = await _attachmentService.GetAttachmentByIdAsync(id, cancellationToken);
        if (attachment == null)
            return NotFound(ApiResponse.Fail("Attachment not found"));

        // Delete file from storage
        await _fileStorageService.DeleteFileAsync(attachment.FilePath, cancellationToken);

        // Soft delete attachment record
        var success = await _attachmentService.DeleteAttachmentAsync(id, userId.Value, cancellationToken);
        if (!success)
            return NotFound(ApiResponse.Fail("Attachment not found"));

        return Ok(ApiResponse.Ok("Attachment deleted successfully"));
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAttachment(int id, CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentService.GetAttachmentByIdAsync(id, cancellationToken);
        if (attachment == null)
            return NotFound();

        var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return File(stream, attachment.MimeType, attachment.FileName);
    }

    [HttpPost("{id}/set-primary")]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> SetPrimaryImage(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<AttachmentDto>.Fail("User not authenticated"));

        var success = await _attachmentService.SetPrimaryImageAsync(id, userId.Value, cancellationToken);
        if (!success)
            return NotFound(ApiResponse<AttachmentDto>.Fail("Attachment not found or is not an image"));

        var attachment = await _attachmentService.GetAttachmentByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<AttachmentDto>.Ok(MapToDto(attachment!), "Primary image set successfully"));
    }

    [HttpGet("primary")]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> GetPrimaryImage(
        [FromQuery] string entityType,
        [FromQuery] int entityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            return BadRequest(ApiResponse<AttachmentDto>.Fail("Entity type and ID are required"));

        var attachment = await _attachmentService.GetPrimaryImageAsync(entityType, entityId, cancellationToken);
        if (attachment == null)
            return Ok(ApiResponse<AttachmentDto>.Ok(null!, "No image found"));

        return Ok(ApiResponse<AttachmentDto>.Ok(MapToDto(attachment)));
    }

    private AttachmentDto MapToDto(Attachment attachment)
    {
        return new AttachmentDto
        {
            Id = attachment.Id,
            EntityType = attachment.EntityType,
            EntityId = attachment.EntityId,
            AttachmentType = attachment.AttachmentType,
            Title = attachment.Title,
            FileName = attachment.FileName,
            FilePath = attachment.FilePath,
            Url = _fileStorageService.GetFileUrl(attachment.FilePath),
            FileSize = attachment.FileSize,
            MimeType = attachment.MimeType,
            Description = attachment.Description,
            DisplayOrder = attachment.DisplayOrder,
            IsPrimary = attachment.IsPrimary,
            UploadedAt = attachment.UploadedAt,
            UploadedBy = attachment.UploadedBy,
            UploadedByName = attachment.Uploader != null
                ? $"{attachment.Uploader.FirstName} {attachment.Uploader.LastName}".Trim()
                : null,
            CreatedAt = attachment.CreatedAt,
            UpdatedAt = attachment.UpdatedAt
        };
    }
}
