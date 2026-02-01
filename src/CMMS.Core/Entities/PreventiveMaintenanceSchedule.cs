using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

/// <summary>
/// Defines a preventive maintenance schedule that generates work orders
/// </summary>
public class PreventiveMaintenanceSchedule : BaseEntity
{
    /// <summary>
    /// Name of the PM schedule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the PM activities
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional asset this schedule applies to (null for asset-type-based)
    /// </summary>
    public int? AssetId { get; set; }

    /// <summary>
    /// How frequently to generate work orders
    /// </summary>
    public FrequencyType FrequencyType { get; set; } = FrequencyType.Monthly;

    /// <summary>
    /// Frequency interval value (e.g., 2 for every 2 weeks)
    /// </summary>
    public int FrequencyValue { get; set; } = 1;

    /// <summary>
    /// Day of week for weekly schedules (0=Sunday, 6=Saturday)
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Day of month for monthly schedules (1-31)
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Next date when a work order should be generated
    /// </summary>
    public DateTime? NextDueDate { get; set; }

    /// <summary>
    /// Date when the last work order was completed
    /// </summary>
    public DateTime? LastCompletedDate { get; set; }

    /// <summary>
    /// Days before due date to generate the work order
    /// </summary>
    public int LeadTimeDays { get; set; }

    /// <summary>
    /// Template title for generated work orders
    /// </summary>
    public string WorkOrderTitle { get; set; } = string.Empty;

    /// <summary>
    /// Template description for generated work orders
    /// </summary>
    public string? WorkOrderDescription { get; set; }

    /// <summary>
    /// Priority for generated work orders
    /// </summary>
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;

    /// <summary>
    /// Estimated hours for generated work orders
    /// </summary>
    public decimal? EstimatedHours { get; set; }

    /// <summary>
    /// Whether this schedule is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Asset? Asset { get; set; }
    public virtual ICollection<WorkOrder> GeneratedWorkOrders { get; set; } = new List<WorkOrder>();
}
