namespace CMMS.Core.Entities;

/// <summary>
/// Represents an individual task item within a template
/// </summary>
public class WorkOrderTaskTemplateItem
{
    public int Id { get; set; }

    /// <summary>
    /// The template this item belongs to
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Sort order for displaying items in sequence
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Description of the task
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this task is required when applied to a work order
    /// </summary>
    public bool IsRequired { get; set; } = true;

    // Navigation properties
    public virtual WorkOrderTaskTemplate Template { get; set; } = null!;
}
