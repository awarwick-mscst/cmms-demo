namespace CMMS.Shared.DTOs;

/// <summary>
/// Full work order task details
/// </summary>
public class WorkOrderTaskDto
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedById { get; set; }
    public string? CompletedByName { get; set; }
    public string? Notes { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Summary of task completion status for a work order
/// </summary>
public class WorkOrderTaskSummaryDto
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int RequiredTasks { get; set; }
    public int CompletedRequiredTasks { get; set; }
    public double CompletionPercentage { get; set; }
    public bool AllRequiredCompleted { get; set; }
}

/// <summary>
/// Request to create a new work order task
/// </summary>
public class CreateWorkOrderTaskRequest
{
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public int? SortOrder { get; set; }
}

/// <summary>
/// Request to update an existing work order task
/// </summary>
public class UpdateWorkOrderTaskRequest
{
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// Request to complete or uncomplete a task
/// </summary>
public class CompleteTaskRequest
{
    public string? Notes { get; set; }
}

/// <summary>
/// Request to reorder tasks
/// </summary>
public class ReorderTasksRequest
{
    public List<int> TaskIds { get; set; } = new();
}

/// <summary>
/// Request to apply a template to a work order
/// </summary>
public class ApplyTemplateRequest
{
    public int TemplateId { get; set; }
    public bool ClearExisting { get; set; } = false;
}
