namespace CMMS.Core.Entities;

/// <summary>
/// Represents an individual task/checklist item on a work order
/// </summary>
public class WorkOrderTask
{
    public int Id { get; set; }

    /// <summary>
    /// The work order this task belongs to
    /// </summary>
    public int WorkOrderId { get; set; }

    /// <summary>
    /// Sort order for displaying tasks in sequence
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Description of the task to be completed
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this task has been completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// When the task was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who completed this task
    /// </summary>
    public int? CompletedById { get; set; }

    /// <summary>
    /// Optional notes added when completing the task
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this task is required to complete the work order
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// When the task was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual WorkOrder WorkOrder { get; set; } = null!;
    public virtual User? CompletedBy { get; set; }
}
