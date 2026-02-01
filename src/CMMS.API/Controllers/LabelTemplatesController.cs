using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/label-templates")]
[Authorize]
public class LabelTemplatesController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly ICurrentUserService _currentUserService;

    public LabelTemplatesController(ILabelService labelService, ICurrentUserService currentUserService)
    {
        _labelService = labelService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LabelTemplateDto>>>> GetTemplates(
        CancellationToken cancellationToken = default)
    {
        var templates = await _labelService.GetTemplatesAsync(cancellationToken);
        var dtos = templates.Select(MapToDto);
        return Ok(ApiResponse<IEnumerable<LabelTemplateDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LabelTemplateDto>>> GetTemplate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var template = await _labelService.GetTemplateByIdAsync(id, cancellationToken);

        if (template == null)
            return NotFound(ApiResponse<LabelTemplateDto>.Fail("Label template not found"));

        return Ok(ApiResponse<LabelTemplateDto>.Ok(MapToDto(template)));
    }

    [HttpGet("default")]
    public async Task<ActionResult<ApiResponse<LabelTemplateDto>>> GetDefaultTemplate(
        CancellationToken cancellationToken = default)
    {
        var template = await _labelService.GetDefaultTemplateAsync(cancellationToken);

        if (template == null)
            return NotFound(ApiResponse<LabelTemplateDto>.Fail("No default label template configured"));

        return Ok(ApiResponse<LabelTemplateDto>.Ok(MapToDto(template)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse<LabelTemplateDto>>> CreateTemplate(
        [FromBody] CreateLabelTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<LabelTemplateDto>.Fail("User not authenticated"));

        var template = new LabelTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Width = request.Width,
            Height = request.Height,
            Dpi = request.Dpi,
            ElementsJson = request.ElementsJson,
            IsDefault = request.IsDefault
        };

        var created = await _labelService.CreateTemplateAsync(template, userId.Value, cancellationToken);

        return CreatedAtAction(
            nameof(GetTemplate),
            new { id = created.Id },
            ApiResponse<LabelTemplateDto>.Ok(MapToDto(created), "Label template created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse<LabelTemplateDto>>> UpdateTemplate(
        int id,
        [FromBody] UpdateLabelTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<LabelTemplateDto>.Fail("User not authenticated"));

        var template = await _labelService.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
            return NotFound(ApiResponse<LabelTemplateDto>.Fail("Label template not found"));

        template.Name = request.Name;
        template.Description = request.Description;
        template.Width = request.Width;
        template.Height = request.Height;
        template.Dpi = request.Dpi;
        template.ElementsJson = request.ElementsJson;
        template.IsDefault = request.IsDefault;

        var updated = await _labelService.UpdateTemplateAsync(template, userId.Value, cancellationToken);

        return Ok(ApiResponse<LabelTemplateDto>.Ok(MapToDto(updated), "Label template updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse>> DeleteTemplate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Fail("User not authenticated"));

        var success = await _labelService.DeleteTemplateAsync(id, userId.Value, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Label template not found"));

        return Ok(ApiResponse.Ok("Label template deleted successfully"));
    }

    [HttpPost("{id}/set-default")]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse>> SetDefaultTemplate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Fail("User not authenticated"));

        var success = await _labelService.SetDefaultTemplateAsync(id, userId.Value, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Label template not found"));

        return Ok(ApiResponse.Ok("Default template set successfully"));
    }

    private static LabelTemplateDto MapToDto(LabelTemplate template)
    {
        return new LabelTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Width = template.Width,
            Height = template.Height,
            Dpi = template.Dpi,
            ElementsJson = template.ElementsJson,
            IsDefault = template.IsDefault,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
