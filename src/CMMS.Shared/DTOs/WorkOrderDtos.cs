namespace CMMS.Shared.DTOs;

/// <summary>
/// Full work order details
/// </summary>
public class WorkOrderDto
{
    public int Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? AssetTag { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? RequestedDate { get; set; }
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public string? CompletionNotes { get; set; }
    public int? PreventiveMaintenanceScheduleId { get; set; }
    public string? PreventiveMaintenanceScheduleName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
}

/// <summary>
/// Summary view for work order lists
/// </summary>
public class WorkOrderSummaryDto
{
    public int Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public string? LocationName { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create a new work order
/// </summary>
public class CreateWorkOrderRequest
{
    public string Type { get; set; } = "Repair";
    public string Priority { get; set; } = "Medium";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AssetId { get; set; }
    public int? LocationId { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? RequestedDate { get; set; }
    public int? AssignedToId { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public decimal? EstimatedHours { get; set; }
}

/// <summary>
/// Request to update an existing work order
/// </summary>
public class UpdateWorkOrderRequest
{
    public string Type { get; set; } = "Repair";
    public string Priority { get; set; } = "Medium";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AssetId { get; set; }
    public int? LocationId { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? RequestedDate { get; set; }
    public int? AssignedToId { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public decimal? EstimatedHours { get; set; }
}

/// <summary>
/// Request to complete a work order
/// </summary>
public class CompleteWorkOrderRequest
{
    public string? CompletionNotes { get; set; }
    public DateTime? ActualEndDate { get; set; }
}

/// <summary>
/// Request to put work order on hold or cancel
/// </summary>
public class WorkOrderStatusChangeRequest
{
    public string? Notes { get; set; }
}

/// <summary>
/// Work order history entry
/// </summary>
public class WorkOrderHistoryDto
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public int ChangedById { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Active work session for time tracking
/// </summary>
public class WorkSessionDto
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string WorkOrderTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public decimal? HoursWorked { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int ElapsedMinutes { get; set; }
}

/// <summary>
/// Request to stop a work session
/// </summary>
public class StopSessionRequest
{
    public string? Notes { get; set; }
}

/// <summary>
/// Request to add a note during an active work session
/// </summary>
public class AddSessionNoteRequest
{
    public string Note { get; set; } = string.Empty;
}
