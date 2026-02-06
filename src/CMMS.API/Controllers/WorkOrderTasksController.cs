using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/work-orders/{workOrderId}/tasks")]
[Authorize]
public class WorkOrderTasksController : ControllerBase
{
    private readonly IWorkOrderTaskService _taskService;
    private readonly ICurrentUserService _currentUserService;

    public WorkOrderTasksController(
        IWorkOrderTaskService taskService,
        ICurrentUserService currentUserService)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkOrderTaskDto>>>> GetTasks(
        int workOrderId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _taskService.GetTasksForWorkOrderAsync(workOrderId, cancellationToken);
        var dtos = tasks.Select(MapToDto);
        return Ok(ApiResponse<IEnumerable<WorkOrderTaskDto>>.Ok(dtos));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<WorkOrderTaskSummaryDto>>> GetTaskSummary(
        int workOrderId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _taskService.GetTaskSummaryAsync(workOrderId, cancellationToken);
        var dto = new WorkOrderTaskSummaryDto
        {
            TotalTasks = summary.TotalTasks,
            CompletedTasks = summary.CompletedTasks,
            RequiredTasks = summary.RequiredTasks,
            CompletedRequiredTasks = summary.CompletedRequiredTasks,
            CompletionPercentage = summary.CompletionPercentage,
            AllRequiredCompleted = summary.AllRequiredCompleted
        };
        return Ok(ApiResponse<WorkOrderTaskSummaryDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderTaskDto>>> CreateTask(
        int workOrderId,
        [FromBody] CreateWorkOrderTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = new WorkOrderTask
        {
            Description = request.Description,
            IsRequired = request.IsRequired,
            SortOrder = request.SortOrder ?? 0
        };

        var created = await _taskService.CreateTaskAsync(workOrderId, task, cancellationToken);
        return CreatedAtAction(
            nameof(GetTasks),
            new { workOrderId },
            ApiResponse<WorkOrderTaskDto>.Ok(MapToDto(created), "Task created successfully"));
    }

    [HttpPut("{taskId}")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderTaskDto>>> UpdateTask(
        int workOrderId,
        int taskId,
        [FromBody] UpdateWorkOrderTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskService.GetTaskByIdAsync(taskId, cancellationToken);
        if (task == null || task.WorkOrderId != workOrderId)
        {
            return NotFound(ApiResponse<WorkOrderTaskDto>.Fail("Task not found"));
        }

        task.Description = request.Description;
        task.IsRequired = request.IsRequired;
        task.Notes = request.Notes;

        var updated = await _taskService.UpdateTaskAsync(task, cancellationToken);
        return Ok(ApiResponse<WorkOrderTaskDto>.Ok(MapToDto(updated), "Task updated successfully"));
    }

    [HttpDelete("{taskId}")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse>> DeleteTask(
        int workOrderId,
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskService.GetTaskByIdAsync(taskId, cancellationToken);
        if (task == null || task.WorkOrderId != workOrderId)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }

        await _taskService.DeleteTaskAsync(taskId, cancellationToken);
        return Ok(ApiResponse.Ok("Task deleted successfully"));
    }

    [HttpPost("{taskId}/complete")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<WorkOrderTaskDto>>> ToggleTaskCompletion(
        int workOrderId,
        int taskId,
        [FromBody] CompleteTaskRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<WorkOrderTaskDto>.Fail("User not authenticated"));
        }

        var task = await _taskService.GetTaskByIdAsync(taskId, cancellationToken);
        if (task == null || task.WorkOrderId != workOrderId)
        {
            return NotFound(ApiResponse<WorkOrderTaskDto>.Fail("Task not found"));
        }

        var updated = await _taskService.ToggleTaskCompletionAsync(taskId, userId.Value, request?.Notes, cancellationToken);
        var message = updated.IsCompleted ? "Task marked as complete" : "Task marked as incomplete";
        return Ok(ApiResponse<WorkOrderTaskDto>.Ok(MapToDto(updated), message));
    }

    [HttpPost("reorder")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse>> ReorderTasks(
        int workOrderId,
        [FromBody] ReorderTasksRequest request,
        CancellationToken cancellationToken = default)
    {
        await _taskService.ReorderTasksAsync(workOrderId, request.TaskIds, cancellationToken);
        return Ok(ApiResponse.Ok("Tasks reordered successfully"));
    }

    [HttpPost("apply-template")]
    [Authorize(Policy = "CanEditWorkOrders")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkOrderTaskDto>>>> ApplyTemplate(
        int workOrderId,
        [FromBody] ApplyTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _taskService.ApplyTemplateAsync(workOrderId, request.TemplateId, request.ClearExisting, cancellationToken);
        var dtos = tasks.Select(MapToDto);
        return Ok(ApiResponse<IEnumerable<WorkOrderTaskDto>>.Ok(dtos, "Template applied successfully"));
    }

    private static WorkOrderTaskDto MapToDto(WorkOrderTask task)
    {
        return new WorkOrderTaskDto
        {
            Id = task.Id,
            WorkOrderId = task.WorkOrderId,
            SortOrder = task.SortOrder,
            Description = task.Description,
            IsCompleted = task.IsCompleted,
            CompletedAt = task.CompletedAt,
            CompletedById = task.CompletedById,
            CompletedByName = task.CompletedBy != null ? $"{task.CompletedBy.FirstName} {task.CompletedBy.LastName}" : null,
            Notes = task.Notes,
            IsRequired = task.IsRequired,
            CreatedAt = task.CreatedAt
        };
    }
}
