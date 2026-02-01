namespace CMMS.Core.Entities;

/// <summary>
/// Comments and notes on work orders
/// </summary>
public class WorkOrderComment
{
    public int Id { get; set; }

    /// <summary>
    /// Work order this comment belongs to
    /// </summary>
    public int WorkOrderId { get; set; }

    /// <summary>
    /// The comment text
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// If true, only visible to technicians/maintenance staff
    /// </summary>
    public bool IsInternal { get; set; }

    /// <summary>
    /// User who created the comment
    /// </summary>
    public int CreatedById { get; set; }

    /// <summary>
    /// When the comment was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual WorkOrder WorkOrder { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
}
