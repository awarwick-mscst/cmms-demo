namespace CMMS.Core.Entities;

/// <summary>
/// Represents a reusable template containing a set of tasks for work orders
/// </summary>
public class WorkOrderTaskTemplate : BaseEntity
{
    /// <summary>
    /// Name of the template
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this template is used for
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this template is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<WorkOrderTaskTemplateItem> Items { get; set; } = new List<WorkOrderTaskTemplateItem>();
    public virtual ICollection<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules { get; set; } = new List<PreventiveMaintenanceSchedule>();
}
