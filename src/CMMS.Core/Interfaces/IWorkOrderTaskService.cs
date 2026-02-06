using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IWorkOrderTaskService
{
    // Task CRUD
    Task<IEnumerable<WorkOrderTask>> GetTasksForWorkOrderAsync(int workOrderId, CancellationToken cancellationToken = default);
    Task<WorkOrderTask?> GetTaskByIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task<WorkOrderTask> CreateTaskAsync(int workOrderId, WorkOrderTask task, CancellationToken cancellationToken = default);
    Task<WorkOrderTask> UpdateTaskAsync(WorkOrderTask task, CancellationToken cancellationToken = default);
    Task<bool> DeleteTaskAsync(int taskId, CancellationToken cancellationToken = default);

    // Task completion
    Task<WorkOrderTask> ToggleTaskCompletionAsync(int taskId, int userId, string? notes = null, CancellationToken cancellationToken = default);

    // Task reordering
    Task ReorderTasksAsync(int workOrderId, IEnumerable<int> taskIds, CancellationToken cancellationToken = default);

    // Template application
    Task<IEnumerable<WorkOrderTask>> ApplyTemplateAsync(int workOrderId, int templateId, bool clearExisting = false, CancellationToken cancellationToken = default);

    // Summary
    Task<WorkOrderTaskSummary> GetTaskSummaryAsync(int workOrderId, CancellationToken cancellationToken = default);
}

public class WorkOrderTaskSummary
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int RequiredTasks { get; set; }
    public int CompletedRequiredTasks { get; set; }
    public double CompletionPercentage { get; set; }
    public bool AllRequiredCompleted { get; set; }
}
