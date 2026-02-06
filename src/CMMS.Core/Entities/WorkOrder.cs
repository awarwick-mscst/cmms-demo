using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

/// <summary>
/// Represents a work order for maintenance activities
/// </summary>
public class WorkOrder : BaseEntity
{
    /// <summary>
    /// Unique work order number (auto-generated: WO-YYYYMMDD-XXXX)
    /// </summary>
    public string WorkOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of work order
    /// </summary>
    public WorkOrderType Type { get; set; } = WorkOrderType.Repair;

    /// <summary>
    /// Priority level
    /// </summary>
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;

    /// <summary>
    /// Current status
    /// </summary>
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

    /// <summary>
    /// Brief title of the work order
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the work to be performed
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional asset this work order relates to
    /// </summary>
    public int? AssetId { get; set; }

    /// <summary>
    /// Location where work is performed
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Name or ID of person who requested the work
    /// </summary>
    public string? RequestedBy { get; set; }

    /// <summary>
    /// Date when the work was requested
    /// </summary>
    public DateTime? RequestedDate { get; set; }

    /// <summary>
    /// User ID of assigned technician
    /// </summary>
    public int? AssignedToId { get; set; }

    /// <summary>
    /// Scheduled start date
    /// </summary>
    public DateTime? ScheduledStartDate { get; set; }

    /// <summary>
    /// Scheduled end date
    /// </summary>
    public DateTime? ScheduledEndDate { get; set; }

    /// <summary>
    /// Actual start date when work began
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// Actual end date when work was completed
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Estimated hours to complete
    /// </summary>
    public decimal? EstimatedHours { get; set; }

    /// <summary>
    /// Actual hours worked (sum of labor entries)
    /// </summary>
    public decimal? ActualHours { get; set; }

    /// <summary>
    /// Notes added when completing the work order
    /// </summary>
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// Link to PM schedule if this work order was generated from one
    /// </summary>
    public int? PreventiveMaintenanceScheduleId { get; set; }

    // Navigation properties
    public virtual Asset? Asset { get; set; }
    public virtual AssetLocation? Location { get; set; }
    public virtual User? AssignedTo { get; set; }
    public virtual PreventiveMaintenanceSchedule? PreventiveMaintenanceSchedule { get; set; }
    public virtual ICollection<WorkOrderHistory> History { get; set; } = new List<WorkOrderHistory>();
    public virtual ICollection<WorkOrderComment> Comments { get; set; } = new List<WorkOrderComment>();
    public virtual ICollection<WorkOrderLabor> LaborEntries { get; set; } = new List<WorkOrderLabor>();
    public virtual ICollection<AssetPart> Parts { get; set; } = new List<AssetPart>();
    public virtual ICollection<WorkOrderTask> Tasks { get; set; } = new List<WorkOrderTask>();
}
