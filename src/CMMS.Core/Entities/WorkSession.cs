namespace CMMS.Core.Entities;

/// <summary>
/// Tracks active work sessions on work orders for automatic time tracking
/// </summary>
public class WorkSession : BaseEntityWithoutAudit
{
    public int WorkOrderId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public decimal? HoursWorked { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual WorkOrder WorkOrder { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
