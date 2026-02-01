using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public WorkOrdersController(
        IWorkOrderService workOrderService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _workOrderService = workOrderService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkOrderSummaryDto>>> GetWorkOrders(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? assetId,
        [FromQuery] int? locationId,
        [FromQuery] int? assignedToId,
        [FromQuery] DateTime? scheduledStartFrom,
        [FromQuery] DateTime? scheduledStartTo,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new WorkOrderFilter
        {
            Search = search,
            Type = type,
            Status = status,
            Priority = priority,
            AssetId = assetId,
            LocationId = locationId,
            AssignedToId = assignedToId,
            ScheduledStartFrom = scheduledStartFrom,
            ScheduledStartTo = scheduledStartTo,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _workOrderService.GetWorkOrdersAsync(filter, cancellationToken);

        var response = new PagedResponse<WorkOrderSummaryDto>
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
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> GetWorkOrder(int id, CancellationToken cancellationToken = default)
    {
        var workOrder = await _workOrderService.GetWorkOrderByIdAsync(id, cancellationToken);

        if (workOrder == null)
        {
            return NotFound(ApiResponse<WorkOrderDto>.Fail("Work order not found"));
        }

        return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(workOrder)));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> CreateWorkOrder(
        [FromBody] CreateWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        var workOrder = new WorkOrder
        {
            Type = Enum.Parse<WorkOrderType>(request.Type),
            Priority = Enum.Parse<WorkOrderPriority>(request.Priority),
            Title = request.Title,
            Description = request.Description,
            AssetId = request.AssetId,
            LocationId = request.LocationId,
            RequestedBy = request.RequestedBy,
            RequestedDate = request.RequestedDate ?? DateTime.UtcNow,
            AssignedToId = request.AssignedToId,
            ScheduledStartDate = request.ScheduledStartDate,
            ScheduledEndDate = request.ScheduledEndDate,
            EstimatedHours = request.EstimatedHours
        };

        var created = await _workOrderService.CreateWorkOrderAsync(workOrder, userId.Value, cancellationToken);

        // Reload with related data
        var result = await _workOrderService.GetWorkOrderByIdAsync(created.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetWorkOrder),
            new { id = created.Id },
            ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> UpdateWorkOrder(
        int id,
        [FromBody] UpdateWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        var workOrder = await _workOrderService.GetWorkOrderByIdAsync(id, cancellationToken);
        if (workOrder == null)
        {
            return NotFound(ApiResponse<WorkOrderDto>.Fail("Work order not found"));
        }

        workOrder.Type = Enum.Parse<WorkOrderType>(request.Type);
        workOrder.Priority = Enum.Parse<WorkOrderPriority>(request.Priority);
        workOrder.Title = request.Title;
        workOrder.Description = request.Description;
        workOrder.AssetId = request.AssetId;
        workOrder.LocationId = request.LocationId;
        workOrder.RequestedBy = request.RequestedBy;
        workOrder.RequestedDate = request.RequestedDate;
        workOrder.AssignedToId = request.AssignedToId;
        workOrder.ScheduledStartDate = request.ScheduledStartDate;
        workOrder.ScheduledEndDate = request.ScheduledEndDate;
        workOrder.EstimatedHours = request.EstimatedHours;

        var updated = await _workOrderService.UpdateWorkOrderAsync(workOrder, userId.Value, cancellationToken);

        // Reload with related data
        var result = await _workOrderService.GetWorkOrderByIdAsync(updated.Id, cancellationToken);

        return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteWorkOrders")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkOrder(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var success = await _workOrderService.DeleteWorkOrderAsync(id, userId.Value, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("Work order not found"));
        }

        return Ok(ApiResponse.Ok("Work order deleted successfully"));
    }

    // Status transitions

    [HttpPost("{id}/submit")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> SubmitWorkOrder(
        int id,
        [FromBody] WorkOrderStatusChangeRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        try
        {
            var workOrder = await _workOrderService.SubmitWorkOrderAsync(id, userId.Value, request?.Notes, cancellationToken);
            var result = await _workOrderService.GetWorkOrderByIdAsync(workOrder.Id, cancellationToken);
            return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order submitted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WorkOrderDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/start")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> StartWorkOrder(
        int id,
        [FromBody] WorkOrderStatusChangeRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        try
        {
            var workOrder = await _workOrderService.StartWorkOrderAsync(id, userId.Value, request?.Notes, cancellationToken);
            var result = await _workOrderService.GetWorkOrderByIdAsync(workOrder.Id, cancellationToken);
            return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order started"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WorkOrderDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/complete")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> CompleteWorkOrder(
        int id,
        [FromBody] CompleteWorkOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        try
        {
            var workOrder = await _workOrderService.CompleteWorkOrderAsync(
                id, userId.Value, request?.CompletionNotes, request?.ActualEndDate, cancellationToken);
            var result = await _workOrderService.GetWorkOrderByIdAsync(workOrder.Id, cancellationToken);
            return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order completed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WorkOrderDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/hold")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> HoldWorkOrder(
        int id,
        [FromBody] WorkOrderStatusChangeRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        try
        {
            var workOrder = await _workOrderService.HoldWorkOrderAsync(id, userId.Value, request?.Notes, cancellationToken);
            var result = await _workOrderService.GetWorkOrderByIdAsync(workOrder.Id, cancellationToken);
            return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order put on hold"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WorkOrderDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/resume")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> ResumeWorkOrder(
        int id,
        [FromBody] WorkOrderStatusChangeRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        try
        {
            var workOrder = await _workOrderService.ResumeWorkOrderAsync(id, userId.Value, request?.Notes, cancellationToken);
            var result = await _workOrderService.GetWorkOrderByIdAsync(workOrder.Id, cancellationToken);
            return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order resumed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WorkOrderDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> CancelWorkOrder(
        int id,
        [FromBody] WorkOrderStatusChangeRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        try
        {
            var workOrder = await _workOrderService.CancelWorkOrderAsync(id, userId.Value, request?.Notes, cancellationToken);
            var result = await _workOrderService.GetWorkOrderByIdAsync(workOrder.Id, cancellationToken);
            return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order cancelled"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WorkOrderDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/reopen")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> ReopenWorkOrder(
        int id,
        [FromBody] WorkOrderStatusChangeRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderDto>.Fail("User not authenticated"));
        }

        try
        {
            var workOrder = await _workOrderService.ReopenWorkOrderAsync(id, userId.Value, request?.Notes, cancellationToken);
            var result = await _workOrderService.GetWorkOrderByIdAsync(workOrder.Id, cancellationToken);
            return Ok(ApiResponse<WorkOrderDto>.Ok(MapToDto(result!), "Work order reopened"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WorkOrderDto>.Fail(ex.Message));
        }
    }

    // History

    [HttpGet("{id}/history")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkOrderHistoryDto>>>> GetWorkOrderHistory(
        int id, CancellationToken cancellationToken = default)
    {
        var history = await _workOrderService.GetWorkOrderHistoryAsync(id, cancellationToken);
        var dtos = history.Select(h => new WorkOrderHistoryDto
        {
            Id = h.Id,
            WorkOrderId = h.WorkOrderId,
            FromStatus = h.FromStatus?.ToString(),
            ToStatus = h.ToStatus.ToString(),
            ChangedById = h.ChangedById,
            ChangedByName = h.ChangedBy?.FullName ?? string.Empty,
            ChangedAt = h.ChangedAt,
            Notes = h.Notes
        });

        return Ok(ApiResponse<IEnumerable<WorkOrderHistoryDto>>.Ok(dtos));
    }

    // Comments

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkOrderCommentDto>>>> GetWorkOrderComments(
        int id,
        [FromQuery] bool includeInternal = false,
        CancellationToken cancellationToken = default)
    {
        var comments = await _workOrderService.GetWorkOrderCommentsAsync(id, includeInternal, cancellationToken);
        var dtos = comments.Select(c => new WorkOrderCommentDto
        {
            Id = c.Id,
            WorkOrderId = c.WorkOrderId,
            Comment = c.Comment,
            IsInternal = c.IsInternal,
            CreatedById = c.CreatedById,
            CreatedByName = c.CreatedBy?.FullName ?? string.Empty,
            CreatedAt = c.CreatedAt
        });

        return Ok(ApiResponse<IEnumerable<WorkOrderCommentDto>>.Ok(dtos));
    }

    [HttpPost("{id}/comments")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderCommentDto>>> AddComment(
        int id,
        [FromBody] CreateWorkOrderCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderCommentDto>.Fail("User not authenticated"));
        }

        var comment = await _workOrderService.AddCommentAsync(id, request.Comment, request.IsInternal, userId.Value, cancellationToken);

        var dto = new WorkOrderCommentDto
        {
            Id = comment.Id,
            WorkOrderId = comment.WorkOrderId,
            Comment = comment.Comment,
            IsInternal = comment.IsInternal,
            CreatedById = comment.CreatedById,
            CreatedByName = _currentUserService.FullName ?? string.Empty,
            CreatedAt = comment.CreatedAt
        };

        return Ok(ApiResponse<WorkOrderCommentDto>.Ok(dto, "Comment added"));
    }

    // Labor

    [HttpGet("{id}/labor")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkOrderLaborDto>>>> GetWorkOrderLabor(
        int id, CancellationToken cancellationToken = default)
    {
        var labor = await _workOrderService.GetWorkOrderLaborAsync(id, cancellationToken);
        var dtos = labor.Select(l => new WorkOrderLaborDto
        {
            Id = l.Id,
            WorkOrderId = l.WorkOrderId,
            UserId = l.UserId,
            UserName = l.User?.FullName ?? string.Empty,
            WorkDate = l.WorkDate,
            HoursWorked = l.HoursWorked,
            LaborType = l.LaborType.ToString(),
            HourlyRate = l.HourlyRate,
            TotalCost = l.HourlyRate.HasValue ? l.HoursWorked * l.HourlyRate.Value : null,
            Notes = l.Notes,
            CreatedAt = l.CreatedAt
        });

        return Ok(ApiResponse<IEnumerable<WorkOrderLaborDto>>.Ok(dtos));
    }

    [HttpPost("{id}/labor")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderLaborDto>>> AddLaborEntry(
        int id,
        [FromBody] CreateWorkOrderLaborRequest request,
        CancellationToken cancellationToken = default)
    {
        var labor = new WorkOrderLabor
        {
            WorkOrderId = id,
            UserId = request.UserId,
            WorkDate = request.WorkDate,
            HoursWorked = request.HoursWorked,
            LaborType = Enum.Parse<LaborType>(request.LaborType),
            HourlyRate = request.HourlyRate,
            Notes = request.Notes
        };

        var created = await _workOrderService.AddLaborEntryAsync(labor, cancellationToken);

        var dto = new WorkOrderLaborDto
        {
            Id = created.Id,
            WorkOrderId = created.WorkOrderId,
            UserId = created.UserId,
            UserName = string.Empty, // Would need to reload to get user name
            WorkDate = created.WorkDate,
            HoursWorked = created.HoursWorked,
            LaborType = created.LaborType.ToString(),
            HourlyRate = created.HourlyRate,
            TotalCost = created.HourlyRate.HasValue ? created.HoursWorked * created.HourlyRate.Value : null,
            Notes = created.Notes,
            CreatedAt = created.CreatedAt
        };

        return Ok(ApiResponse<WorkOrderLaborDto>.Ok(dto, "Labor entry added"));
    }

    [HttpDelete("{id}/labor/{laborId}")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse>> DeleteLaborEntry(
        int id,
        int laborId,
        CancellationToken cancellationToken = default)
    {
        var success = await _workOrderService.DeleteLaborEntryAsync(id, laborId, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("Labor entry not found"));
        }

        return Ok(ApiResponse.Ok("Labor entry deleted"));
    }

    [HttpGet("{id}/labor/summary")]
    public async Task<ActionResult<ApiResponse<WorkOrderLaborSummary>>> GetLaborSummary(
        int id, CancellationToken cancellationToken = default)
    {
        var summary = await _workOrderService.GetLaborSummaryAsync(id, cancellationToken);
        return Ok(ApiResponse<WorkOrderLaborSummary>.Ok(summary));
    }

    // Parts

    [HttpGet("{id}/parts")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AssetPartDto>>>> GetWorkOrderParts(
        int id, CancellationToken cancellationToken = default)
    {
        var parts = await _workOrderService.GetWorkOrderPartsAsync(id, cancellationToken);
        var dtos = parts.Select(p => new AssetPartDto
        {
            Id = p.Id,
            AssetId = p.AssetId,
            AssetTag = p.Asset?.AssetTag ?? string.Empty,
            AssetName = p.Asset?.Name ?? string.Empty,
            PartId = p.PartId,
            PartNumber = p.Part?.PartNumber ?? string.Empty,
            PartName = p.Part?.Name ?? string.Empty,
            QuantityUsed = p.QuantityUsed,
            UnitCostAtTime = p.UnitCostAtTime,
            TotalCost = p.TotalCost,
            UsedDate = p.UsedDate,
            UsedBy = p.UsedBy,
            UsedByName = p.UsedByUser?.FullName,
            WorkOrderId = p.WorkOrderId,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt
        });

        return Ok(ApiResponse<IEnumerable<AssetPartDto>>.Ok(dtos));
    }

    // Dashboard

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<WorkOrderDashboard>>> GetDashboard(CancellationToken cancellationToken = default)
    {
        var dashboard = await _workOrderService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResponse<WorkOrderDashboard>.Ok(dashboard));
    }

    // Work Sessions (Active Time Tracking)

    /// <summary>
    /// Start working on a work order (clock in)
    /// </summary>
    [HttpPost("{id}/start-session")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkSessionDto>>> StartWorkSession(
        int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkSessionDto>.Fail("User not authenticated"));
        }

        // Check if user already has an active session
        var existingSession = await _unitOfWork.WorkSessions.Query()
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.IsActive, cancellationToken);

        if (existingSession != null)
        {
            return BadRequest(ApiResponse<WorkSessionDto>.Fail(
                $"You already have an active session on work order #{existingSession.WorkOrderId}. Please end that session first."));
        }

        // Get the work order
        var workOrder = await _workOrderService.GetWorkOrderByIdAsync(id, cancellationToken);
        if (workOrder == null)
        {
            return NotFound(ApiResponse<WorkSessionDto>.Fail("Work order not found"));
        }

        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            return BadRequest(ApiResponse<WorkSessionDto>.Fail("Work order must be In Progress to start a work session"));
        }

        var session = new WorkSession
        {
            WorkOrderId = id,
            UserId = userId.Value,
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.WorkSessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        session = await _unitOfWork.WorkSessions.Query()
            .Include(s => s.WorkOrder)
            .Include(s => s.User)
            .FirstAsync(s => s.Id == session.Id, cancellationToken);

        return Ok(ApiResponse<WorkSessionDto>.Ok(MapToSessionDto(session), "Work session started"));
    }

    /// <summary>
    /// Stop working on a work order (clock out)
    /// </summary>
    [HttpPost("{id}/stop-session")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkSessionDto>>> StopWorkSession(
        int id,
        [FromBody] StopSessionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkSessionDto>.Fail("User not authenticated"));
        }

        var session = await _unitOfWork.WorkSessions.Query()
            .Include(s => s.WorkOrder)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.WorkOrderId == id && s.UserId == userId.Value && s.IsActive, cancellationToken);

        if (session == null)
        {
            return NotFound(ApiResponse<WorkSessionDto>.Fail("No active session found for this work order"));
        }

        session.EndedAt = DateTime.UtcNow;
        session.IsActive = false;
        session.HoursWorked = (decimal)(session.EndedAt.Value - session.StartedAt).TotalHours;
        session.Notes = request?.Notes;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Automatically create labor entry if hours worked > 0
        if (session.HoursWorked > 0)
        {
            var laborEntry = new WorkOrderLabor
            {
                WorkOrderId = id,
                UserId = userId.Value,
                WorkDate = session.StartedAt.Date,
                HoursWorked = Math.Round(session.HoursWorked.Value, 2),
                LaborType = LaborType.Regular,
                Notes = $"Auto-logged from work session. {request?.Notes}".Trim()
            };

            await _unitOfWork.WorkOrderLabor.AddAsync(laborEntry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<WorkSessionDto>.Ok(MapToSessionDto(session),
            $"Work session ended. {session.HoursWorked:F2} hours logged."));
    }

    /// <summary>
    /// Get active session for a work order
    /// </summary>
    [HttpGet("{id}/active-session")]
    public async Task<ActionResult<ApiResponse<WorkSessionDto?>>> GetActiveSession(
        int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkSessionDto?>.Fail("User not authenticated"));
        }

        var session = await _unitOfWork.WorkSessions.Query()
            .Include(s => s.WorkOrder)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.WorkOrderId == id && s.UserId == userId.Value && s.IsActive, cancellationToken);

        return Ok(ApiResponse<WorkSessionDto?>.Ok(session != null ? MapToSessionDto(session) : null));
    }

    /// <summary>
    /// Get current user's active session (any work order)
    /// </summary>
    [HttpGet("my-active-session")]
    public async Task<ActionResult<ApiResponse<WorkSessionDto?>>> GetMyActiveSession(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkSessionDto?>.Fail("User not authenticated"));
        }

        var session = await _unitOfWork.WorkSessions.Query()
            .Include(s => s.WorkOrder)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.IsActive, cancellationToken);

        return Ok(ApiResponse<WorkSessionDto?>.Ok(session != null ? MapToSessionDto(session) : null));
    }

    /// <summary>
    /// Get all active work sessions (for availability widget)
    /// </summary>
    [HttpGet("active-sessions")]
    public async Task<ActionResult<ApiResponse<List<WorkSessionDto>>>> GetAllActiveSessions(
        CancellationToken cancellationToken = default)
    {
        var sessions = await _unitOfWork.WorkSessions.Query()
            .Include(s => s.WorkOrder)
            .Include(s => s.User)
            .Where(s => s.IsActive)
            .OrderBy(s => s.User!.FirstName)
            .ThenBy(s => s.User!.LastName)
            .ToListAsync(cancellationToken);

        var dtos = sessions.Select(MapToSessionDto).ToList();

        return Ok(ApiResponse<List<WorkSessionDto>>.Ok(dtos));
    }

    /// <summary>
    /// Add notes to current active session
    /// </summary>
    [HttpPost("{id}/session-note")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<object>>> AddSessionNote(
        int id,
        [FromBody] AddSessionNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<object>.Fail("User not authenticated"));
        }

        var session = await _unitOfWork.WorkSessions.Query()
            .FirstOrDefaultAsync(s => s.WorkOrderId == id && s.UserId == userId.Value && s.IsActive, cancellationToken);

        if (session == null)
        {
            return NotFound(ApiResponse<object>.Fail("No active session found"));
        }

        // Append note with timestamp
        var timestamp = DateTime.UtcNow.ToString("HH:mm");
        var newNote = $"[{timestamp}] {request.Note}";
        session.Notes = string.IsNullOrEmpty(session.Notes)
            ? newNote
            : $"{session.Notes}\n{newNote}";

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Note added"));
    }

    private static WorkSessionDto MapToSessionDto(WorkSession session)
    {
        return new WorkSessionDto
        {
            Id = session.Id,
            WorkOrderId = session.WorkOrderId,
            WorkOrderNumber = session.WorkOrder?.WorkOrderNumber ?? "",
            WorkOrderTitle = session.WorkOrder?.Title ?? "",
            UserId = session.UserId,
            UserName = session.User?.FullName ?? "",
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            HoursWorked = session.HoursWorked,
            Notes = session.Notes,
            IsActive = session.IsActive,
            ElapsedMinutes = session.IsActive
                ? (int)(DateTime.UtcNow - session.StartedAt).TotalMinutes
                : (session.EndedAt.HasValue ? (int)(session.EndedAt.Value - session.StartedAt).TotalMinutes : 0)
        };
    }

    // Mapping helpers

    private static WorkOrderDto MapToDto(WorkOrder workOrder)
    {
        return new WorkOrderDto
        {
            Id = workOrder.Id,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            Type = workOrder.Type.ToString(),
            Priority = workOrder.Priority.ToString(),
            Status = workOrder.Status.ToString(),
            Title = workOrder.Title,
            Description = workOrder.Description,
            AssetId = workOrder.AssetId,
            AssetName = workOrder.Asset?.Name,
            AssetTag = workOrder.Asset?.AssetTag,
            LocationId = workOrder.LocationId,
            LocationName = workOrder.Location?.FullPath ?? workOrder.Location?.Name,
            RequestedBy = workOrder.RequestedBy,
            RequestedDate = workOrder.RequestedDate,
            AssignedToId = workOrder.AssignedToId,
            AssignedToName = workOrder.AssignedTo?.FullName,
            ScheduledStartDate = workOrder.ScheduledStartDate,
            ScheduledEndDate = workOrder.ScheduledEndDate,
            ActualStartDate = workOrder.ActualStartDate,
            ActualEndDate = workOrder.ActualEndDate,
            EstimatedHours = workOrder.EstimatedHours,
            ActualHours = workOrder.ActualHours,
            CompletionNotes = workOrder.CompletionNotes,
            PreventiveMaintenanceScheduleId = workOrder.PreventiveMaintenanceScheduleId,
            PreventiveMaintenanceScheduleName = workOrder.PreventiveMaintenanceSchedule?.Name,
            CreatedAt = workOrder.CreatedAt,
            UpdatedAt = workOrder.UpdatedAt
        };
    }

    private static WorkOrderSummaryDto MapToSummaryDto(WorkOrder workOrder)
    {
        return new WorkOrderSummaryDto
        {
            Id = workOrder.Id,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            Type = workOrder.Type.ToString(),
            Priority = workOrder.Priority.ToString(),
            Status = workOrder.Status.ToString(),
            Title = workOrder.Title,
            AssetName = workOrder.Asset?.Name,
            LocationName = workOrder.Location?.FullPath ?? workOrder.Location?.Name,
            AssignedToName = workOrder.AssignedTo?.FullName,
            ScheduledStartDate = workOrder.ScheduledStartDate,
            ScheduledEndDate = workOrder.ScheduledEndDate,
            CreatedAt = workOrder.CreatedAt
        };
    }
}
