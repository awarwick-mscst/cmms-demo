using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

/// <summary>
/// Tracks status changes for work orders
/// </summary>
public class WorkOrderHistory
{
    public int Id { get; set; }

    /// <summary>
    /// Work order this history entry belongs to
    /// </summary>
    public int WorkOrderId { get; set; }

    /// <summary>
    /// Previous status before the change
    /// </summary>
    public WorkOrderStatus? FromStatus { get; set; }

    /// <summary>
    /// New status after the change
    /// </summary>
    public WorkOrderStatus ToStatus { get; set; }

    /// <summary>
    /// User who made the change
    /// </summary>
    public int ChangedById { get; set; }

    /// <summary>
    /// When the change occurred
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional notes about the status change
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual WorkOrder WorkOrder { get; set; } = null!;
    public virtual User ChangedBy { get; set; } = null!;
}
