using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

/// <summary>
/// Labor time entries for work orders
/// </summary>
public class WorkOrderLabor
{
    public int Id { get; set; }

    /// <summary>
    /// Work order this labor entry belongs to
    /// </summary>
    public int WorkOrderId { get; set; }

    /// <summary>
    /// Technician who performed the work
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Date when the work was performed
    /// </summary>
    public DateTime WorkDate { get; set; }

    /// <summary>
    /// Number of hours worked
    /// </summary>
    public decimal HoursWorked { get; set; }

    /// <summary>
    /// Type of labor (Regular, Overtime, Emergency)
    /// </summary>
    public LaborType LaborType { get; set; } = LaborType.Regular;

    /// <summary>
    /// Hourly rate for this labor entry
    /// </summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>
    /// Optional notes about the work performed
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When this entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual WorkOrder WorkOrder { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
