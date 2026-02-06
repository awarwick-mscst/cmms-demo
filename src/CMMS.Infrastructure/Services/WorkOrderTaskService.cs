using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class WorkOrderTaskService : IWorkOrderTaskService
{
    private readonly IUnitOfWork _unitOfWork;

    public WorkOrderTaskService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<WorkOrderTask>> GetTasksForWorkOrderAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrderTasks.Query()
            .Include(t => t.CompletedBy)
            .Where(t => t.WorkOrderId == workOrderId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderTask?> GetTaskByIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrderTasks.Query()
            .Include(t => t.CompletedBy)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
    }

    public async Task<WorkOrderTask> CreateTaskAsync(int workOrderId, WorkOrderTask task, CancellationToken cancellationToken = default)
    {
        task.WorkOrderId = workOrderId;
        task.CreatedAt = DateTime.UtcNow;

        // If no sort order specified, add at the end
        if (task.SortOrder == 0)
        {
            var maxOrder = await _unitOfWork.WorkOrderTasks.Query()
                .Where(t => t.WorkOrderId == workOrderId)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;
            task.SortOrder = maxOrder + 1;
        }

        await _unitOfWork.WorkOrderTasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task;
    }

    public async Task<WorkOrderTask> UpdateTaskAsync(WorkOrderTask task, CancellationToken cancellationToken = default)
    {
        _unitOfWork.WorkOrderTasks.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<bool> DeleteTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.WorkOrderTasks.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            return false;
        }

        _unitOfWork.WorkOrderTasks.Remove(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WorkOrderTask> ToggleTaskCompletionAsync(int taskId, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.WorkOrderTasks.Query()
            .Include(t => t.CompletedBy)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (task.IsCompleted)
        {
            // Uncomplete the task
            task.IsCompleted = false;
            task.CompletedAt = null;
            task.CompletedById = null;
        }
        else
        {
            // Complete the task
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedById = userId;
        }

        if (notes != null)
        {
            task.Notes = notes;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to get the CompletedBy user
        return (await GetTaskByIdAsync(taskId, cancellationToken))!;
    }

    public async Task ReorderTasksAsync(int workOrderId, IEnumerable<int> taskIds, CancellationToken cancellationToken = default)
    {
        var tasks = await _unitOfWork.WorkOrderTasks.Query()
            .Where(t => t.WorkOrderId == workOrderId)
            .ToListAsync(cancellationToken);

        var taskIdsList = taskIds.ToList();
        for (int i = 0; i < taskIdsList.Count; i++)
        {
            var task = tasks.FirstOrDefault(t => t.Id == taskIdsList[i]);
            if (task != null)
            {
                task.SortOrder = i + 1;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkOrderTask>> ApplyTemplateAsync(int workOrderId, int templateId, bool clearExisting = false, CancellationToken cancellationToken = default)
    {
        var template = await _unitOfWork.WorkOrderTaskTemplates.Query()
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException("Template not found");
        }

        if (clearExisting)
        {
            var existingTasks = await _unitOfWork.WorkOrderTasks.Query()
                .Where(t => t.WorkOrderId == workOrderId)
                .ToListAsync(cancellationToken);

            foreach (var task in existingTasks)
            {
                _unitOfWork.WorkOrderTasks.Remove(task);
            }
        }

        // Get the current max sort order
        var maxOrder = clearExisting ? 0 : await _unitOfWork.WorkOrderTasks.Query()
            .Where(t => t.WorkOrderId == workOrderId)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var newTasks = new List<WorkOrderTask>();
        foreach (var item in template.Items.OrderBy(i => i.SortOrder))
        {
            var task = new WorkOrderTask
            {
                WorkOrderId = workOrderId,
                SortOrder = ++maxOrder,
                Description = item.Description,
                IsRequired = item.IsRequired,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.WorkOrderTasks.AddAsync(task, cancellationToken);
            newTasks.Add(task);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return newTasks;
    }

    public async Task<WorkOrderTaskSummary> GetTaskSummaryAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var tasks = await _unitOfWork.WorkOrderTasks.Query()
            .Where(t => t.WorkOrderId == workOrderId)
            .ToListAsync(cancellationToken);

        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.IsCompleted);
        var requiredTasks = tasks.Count(t => t.IsRequired);
        var completedRequiredTasks = tasks.Count(t => t.IsRequired && t.IsCompleted);

        return new WorkOrderTaskSummary
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            RequiredTasks = requiredTasks,
            CompletedRequiredTasks = completedRequiredTasks,
            CompletionPercentage = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0,
            AllRequiredCompleted = requiredTasks == completedRequiredTasks
        };
    }
}
