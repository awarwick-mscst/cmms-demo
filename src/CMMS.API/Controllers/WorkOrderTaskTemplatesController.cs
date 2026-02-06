using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/work-order-task-templates")]
[Authorize]
public class WorkOrderTaskTemplatesController : ControllerBase
{
    private readonly IWorkOrderTaskTemplateService _templateService;
    private readonly ICurrentUserService _currentUserService;

    public WorkOrderTaskTemplatesController(
        IWorkOrderTaskTemplateService templateService,
        ICurrentUserService currentUserService)
    {
        _templateService = templateService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkOrderTaskTemplateSummaryDto>>> GetTemplates(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new WorkOrderTaskTemplateFilter
        {
            Search = search,
            IsActive = isActive,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _templateService.GetTemplatesAsync(filter, cancellationToken);

        var response = new PagedResponse<WorkOrderTaskTemplateSummaryDto>
        {
            Items = result.Items.Select(MapToSummaryDto),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        };

        return Ok(response);
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskTemplateDropdownDto>>>> GetActiveTemplates(
        CancellationToken cancellationToken = default)
    {
        var templates = await _templateService.GetActiveTemplatesAsync(cancellationToken);
        var dtos = templates.Select(t => new TaskTemplateDropdownDto
        {
            Id = t.Id,
            Name = t.Name,
            ItemCount = t.Items.Count
        });
        return Ok(ApiResponse<IEnumerable<TaskTemplateDropdownDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WorkOrderTaskTemplateDto>>> GetTemplate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateService.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
        {
            return NotFound(ApiResponse<WorkOrderTaskTemplateDto>.Fail("Template not found"));
        }

        return Ok(ApiResponse<WorkOrderTaskTemplateDto>.Ok(MapToDto(template)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse<WorkOrderTaskTemplateDto>>> CreateTemplate(
        [FromBody] CreateWorkOrderTaskTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderTaskTemplateDto>.Fail("User not authenticated"));
        }

        var template = new WorkOrderTaskTemplate
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        var items = request.Items.Select((item, index) => new WorkOrderTaskTemplateItem
        {
            SortOrder = item.SortOrder > 0 ? item.SortOrder : index,
            Description = item.Description,
            IsRequired = item.IsRequired
        });

        var created = await _templateService.CreateTemplateAsync(template, items, userId.Value, cancellationToken);

        return CreatedAtAction(
            nameof(GetTemplate),
            new { id = created.Id },
            ApiResponse<WorkOrderTaskTemplateDto>.Ok(MapToDto(created), "Template created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse<WorkOrderTaskTemplateDto>>> UpdateTemplate(
        int id,
        [FromBody] UpdateWorkOrderTaskTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderTaskTemplateDto>.Fail("User not authenticated"));
        }

        var existingTemplate = await _templateService.GetTemplateByIdAsync(id, cancellationToken);
        if (existingTemplate == null)
        {
            return NotFound(ApiResponse<WorkOrderTaskTemplateDto>.Fail("Template not found"));
        }

        var template = new WorkOrderTaskTemplate
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        var items = request.Items.Select((item, index) => new WorkOrderTaskTemplateItem
        {
            Id = item.Id ?? 0,
            SortOrder = item.SortOrder > 0 ? item.SortOrder : index,
            Description = item.Description,
            IsRequired = item.IsRequired
        });

        var updated = await _templateService.UpdateTemplateAsync(template, items, userId.Value, cancellationToken);

        return Ok(ApiResponse<WorkOrderTaskTemplateDto>.Ok(MapToDto(updated), "Template updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse>> DeleteTemplate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var success = await _templateService.DeleteTemplateAsync(id, userId.Value, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponse.Fail("Template not found"));
        }

        return Ok(ApiResponse.Ok("Template deleted successfully"));
    }

    private static WorkOrderTaskTemplateDto MapToDto(WorkOrderTaskTemplate template)
    {
        return new WorkOrderTaskTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
            ItemCount = template.Items.Count,
            Items = template.Items.OrderBy(i => i.SortOrder).Select(i => new WorkOrderTaskTemplateItemDto
            {
                Id = i.Id,
                SortOrder = i.SortOrder,
                Description = i.Description,
                IsRequired = i.IsRequired
            }).ToList(),
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static WorkOrderTaskTemplateSummaryDto MapToSummaryDto(WorkOrderTaskTemplate template)
    {
        return new WorkOrderTaskTemplateSummaryDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
            ItemCount = template.Items.Count,
            CreatedAt = template.CreatedAt
        };
    }
}
