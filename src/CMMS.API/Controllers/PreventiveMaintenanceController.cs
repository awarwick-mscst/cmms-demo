using CMMS.API.Attributes;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/preventive-maintenance")]
[Authorize]
[RequiresFeature("preventive-maintenance")]
public class PreventiveMaintenanceController : ControllerBase
{
    private readonly IPreventiveMaintenanceService _pmService;
    private readonly ICurrentUserService _currentUserService;

    public PreventiveMaintenanceController(
        IPreventiveMaintenanceService pmService,
        ICurrentUserService currentUserService)
    {
        _pmService = pmService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<PreventiveMaintenanceScheduleSummaryDto>>> GetSchedules(
        [FromQuery] string? search,
        [FromQuery] int? assetId,
        [FromQuery] string? frequencyType,
        [FromQuery] bool? isActive,
        [FromQuery] DateTime? dueBefore,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new PreventiveMaintenanceScheduleFilter
        {
            Search = search,
            AssetId = assetId,
            FrequencyType = frequencyType,
            IsActive = isActive,
            DueBefore = dueBefore,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _pmService.GetSchedulesAsync(filter, cancellationToken);

        var response = new PagedResponse<PreventiveMaintenanceScheduleSummaryDto>
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

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PreventiveMaintenanceScheduleDto>>> GetSchedule(
        int id, CancellationToken cancellationToken = default)
    {
        var schedule = await _pmService.GetScheduleByIdAsync(id, cancellationToken);

        if (schedule == null)
        {
            return NotFound(ApiResponse<PreventiveMaintenanceScheduleDto>.Fail("PM schedule not found"));
        }

        return Ok(ApiResponse<PreventiveMaintenanceScheduleDto>.Ok(MapToDto(schedule)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse<PreventiveMaintenanceScheduleDto>>> CreateSchedule(
        [FromBody] CreatePreventiveMaintenanceScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<PreventiveMaintenanceScheduleDto>.Fail("User not authenticated"));
        }

        var schedule = new PreventiveMaintenanceSchedule
        {
            Name = request.Name,
            Description = request.Description,
            AssetId = request.AssetId,
            FrequencyType = Enum.Parse<FrequencyType>(request.FrequencyType),
            FrequencyValue = request.FrequencyValue,
            DayOfWeek = request.DayOfWeek,
            DayOfMonth = request.DayOfMonth,
            NextDueDate = request.NextDueDate,
            LeadTimeDays = request.LeadTimeDays,
            WorkOrderTitle = request.WorkOrderTitle,
            WorkOrderDescription = request.WorkOrderDescription,
            Priority = Enum.Parse<WorkOrderPriority>(request.Priority),
            EstimatedHours = request.EstimatedHours,
            IsActive = request.IsActive,
            TaskTemplateId = request.TaskTemplateId
        };

        var created = await _pmService.CreateScheduleAsync(schedule, userId.Value, cancellationToken);

        // Reload with related data
        var result = await _pmService.GetScheduleByIdAsync(created.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetSchedule),
            new { id = created.Id },
            ApiResponse<PreventiveMaintenanceScheduleDto>.Ok(MapToDto(result!), "PM schedule created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse<PreventiveMaintenanceScheduleDto>>> UpdateSchedule(
        int id,
        [FromBody] UpdatePreventiveMaintenanceScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<PreventiveMaintenanceScheduleDto>.Fail("User not authenticated"));
        }

        var schedule = await _pmService.GetScheduleByIdAsync(id, cancellationToken);
        if (schedule == null)
        {
            return NotFound(ApiResponse<PreventiveMaintenanceScheduleDto>.Fail("PM schedule not found"));
        }

        schedule.Name = request.Name;
        schedule.Description = request.Description;
        schedule.AssetId = request.AssetId;
        schedule.FrequencyType = Enum.Parse<FrequencyType>(request.FrequencyType);
        schedule.FrequencyValue = request.FrequencyValue;
        schedule.DayOfWeek = request.DayOfWeek;
        schedule.DayOfMonth = request.DayOfMonth;
        schedule.NextDueDate = request.NextDueDate;
        schedule.LeadTimeDays = request.LeadTimeDays;
        schedule.WorkOrderTitle = request.WorkOrderTitle;
        schedule.WorkOrderDescription = request.WorkOrderDescription;
        schedule.Priority = Enum.Parse<WorkOrderPriority>(request.Priority);
        schedule.EstimatedHours = request.EstimatedHours;
        schedule.IsActive = request.IsActive;
        schedule.TaskTemplateId = request.TaskTemplateId;

        var updated = await _pmService.UpdateScheduleAsync(schedule, userId.Value, cancellationToken);

        // Reload with related data
        var result = await _pmService.GetScheduleByIdAsync(updated.Id, cancellationToken);

        return Ok(ApiResponse<PreventiveMaintenanceScheduleDto>.Ok(MapToDto(result!), "PM schedule updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse>> DeleteSchedule(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var success = await _pmService.DeleteScheduleAsync(id, userId.Value, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("PM schedule not found"));
        }

        return Ok(ApiResponse.Ok("PM schedule deleted successfully"));
    }

    [HttpPost("generate")]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse<GenerateWorkOrdersResult>>> GenerateDueWorkOrders(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<GenerateWorkOrdersResult>.Fail("User not authenticated"));
        }

        var result = await _pmService.GenerateDueWorkOrdersAsync(userId.Value, cancellationToken);

        return Ok(ApiResponse<GenerateWorkOrdersResult>.Ok(result,
            $"Generated {result.WorkOrdersCreated} work orders from {result.SchedulesProcessed} schedules"));
    }

    [HttpPost("{id}/generate")]
    [Authorize(Policy = "CanManagePreventiveMaintenance")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> GenerateWorkOrderForSchedule(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        var workOrder = await _pmService.GenerateWorkOrderForScheduleAsync(id, userId.Value, cancellationToken);

        if (workOrder == null)
        {
            return NotFound(ApiResponse<WorkOrderDto>.Fail("PM schedule not found or not active"));
        }

        return Ok(ApiResponse<WorkOrderDto>.Ok(new WorkOrderDto
        {
            Id = workOrder.Id,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            Title = workOrder.Title,
            Status = workOrder.Status.ToString()
        }, "Work order generated"));
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UpcomingMaintenance>>>> GetUpcomingMaintenance(
        [FromQuery] int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var upcoming = await _pmService.GetUpcomingMaintenanceAsync(Math.Min(daysAhead, 365), cancellationToken);
        return Ok(ApiResponse<IEnumerable<UpcomingMaintenance>>.Ok(upcoming));
    }

    // Mapping helpers

    private static PreventiveMaintenanceScheduleDto MapToDto(PreventiveMaintenanceSchedule schedule)
    {
        return new PreventiveMaintenanceScheduleDto
        {
            Id = schedule.Id,
            Name = schedule.Name,
            Description = schedule.Description,
            AssetId = schedule.AssetId,
            AssetName = schedule.Asset?.Name,
            AssetTag = schedule.Asset?.AssetTag,
            FrequencyType = schedule.FrequencyType.ToString(),
            FrequencyValue = schedule.FrequencyValue,
            DayOfWeek = schedule.DayOfWeek,
            DayOfMonth = schedule.DayOfMonth,
            NextDueDate = schedule.NextDueDate,
            LastCompletedDate = schedule.LastCompletedDate,
            LeadTimeDays = schedule.LeadTimeDays,
            WorkOrderTitle = schedule.WorkOrderTitle,
            WorkOrderDescription = schedule.WorkOrderDescription,
            Priority = schedule.Priority.ToString(),
            EstimatedHours = schedule.EstimatedHours,
            IsActive = schedule.IsActive,
            TaskTemplateId = schedule.TaskTemplateId,
            TaskTemplateName = schedule.TaskTemplate?.Name,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
    }

    private static PreventiveMaintenanceScheduleSummaryDto MapToSummaryDto(PreventiveMaintenanceSchedule schedule)
    {
        return new PreventiveMaintenanceScheduleSummaryDto
        {
            Id = schedule.Id,
            Name = schedule.Name,
            AssetName = schedule.Asset?.Name,
            FrequencyType = schedule.FrequencyType.ToString(),
            FrequencyValue = schedule.FrequencyValue,
            NextDueDate = schedule.NextDueDate,
            LastCompletedDate = schedule.LastCompletedDate,
            Priority = schedule.Priority.ToString(),
            IsActive = schedule.IsActive
        };
    }
}
