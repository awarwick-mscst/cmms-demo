using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public WorkOrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<WorkOrder>> GetWorkOrdersAsync(WorkOrderFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.WorkOrders.Query()
            .Include(w => w.Asset)
            .Include(w => w.Location)
            .Include(w => w.AssignedTo)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(w =>
                w.Title.ToLower().Contains(search) ||
                w.WorkOrderNumber.ToLower().Contains(search) ||
                (w.Description != null && w.Description.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Type) && Enum.TryParse<WorkOrderType>(filter.Type, out var type))
        {
            query = query.Where(w => w.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<WorkOrderStatus>(filter.Status, out var status))
        {
            query = query.Where(w => w.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Priority) && Enum.TryParse<WorkOrderPriority>(filter.Priority, out var priority))
        {
            query = query.Where(w => w.Priority == priority);
        }

        if (filter.AssetId.HasValue)
        {
            query = query.Where(w => w.AssetId == filter.AssetId.Value);
        }

        if (filter.LocationId.HasValue)
        {
            query = query.Where(w => w.LocationId == filter.LocationId.Value);
        }

        if (filter.AssignedToId.HasValue)
        {
            query = query.Where(w => w.AssignedToId == filter.AssignedToId.Value);
        }

        if (filter.ScheduledStartFrom.HasValue)
        {
            query = query.Where(w => w.ScheduledStartDate >= filter.ScheduledStartFrom.Value);
        }

        if (filter.ScheduledStartTo.HasValue)
        {
            query = query.Where(w => w.ScheduledStartDate <= filter.ScheduledStartTo.Value);
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= filter.CreatedTo.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "title" => filter.SortDescending ? query.OrderByDescending(w => w.Title) : query.OrderBy(w => w.Title),
            "workordernumber" => filter.SortDescending ? query.OrderByDescending(w => w.WorkOrderNumber) : query.OrderBy(w => w.WorkOrderNumber),
            "status" => filter.SortDescending ? query.OrderByDescending(w => w.Status) : query.OrderBy(w => w.Status),
            "priority" => filter.SortDescending ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
            "type" => filter.SortDescending ? query.OrderByDescending(w => w.Type) : query.OrderBy(w => w.Type),
            "scheduledstartdate" => filter.SortDescending ? query.OrderByDescending(w => w.ScheduledStartDate) : query.OrderBy(w => w.ScheduledStartDate),
            "createdat" => filter.SortDescending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt),
            _ => query.OrderByDescending(w => w.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkOrder>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<WorkOrder?> GetWorkOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrders.Query()
            .Include(w => w.Asset)
            .Include(w => w.Location)
            .Include(w => w.AssignedTo)
            .Include(w => w.PreventiveMaintenanceSchedule)
            .Include(w => w.History.OrderByDescending(h => h.ChangedAt))
                .ThenInclude(h => h.ChangedBy)
            .Include(w => w.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.CreatedBy)
            .Include(w => w.LaborEntries.OrderByDescending(l => l.WorkDate))
                .ThenInclude(l => l.User)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WorkOrder?> GetWorkOrderByNumberAsync(string workOrderNumber, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrders.Query()
            .Include(w => w.Asset)
            .Include(w => w.Location)
            .Include(w => w.AssignedTo)
            .FirstOrDefaultAsync(w => w.WorkOrderNumber == workOrderNumber, cancellationToken);
    }

    public async Task<WorkOrder> CreateWorkOrderAsync(WorkOrder workOrder, int createdBy, CancellationToken cancellationToken = default)
    {
        workOrder.CreatedBy = createdBy;
        workOrder.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(workOrder.WorkOrderNumber))
        {
            workOrder.WorkOrderNumber = await GenerateWorkOrderNumberAsync(cancellationToken);
        }

        // Initial status is Draft unless specified
        if (workOrder.Status == default)
        {
            workOrder.Status = WorkOrderStatus.Draft;
        }

        await _unitOfWork.WorkOrders.AddAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add history entry for creation
        var history = new WorkOrderHistory
        {
            WorkOrderId = workOrder.Id,
            FromStatus = null,
            ToStatus = workOrder.Status,
            ChangedById = createdBy,
            ChangedAt = DateTime.UtcNow,
            Notes = "Work order created"
        };
        await _unitOfWork.WorkOrderHistory.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workOrder;
    }

    public async Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder, int updatedBy, CancellationToken cancellationToken = default)
    {
        workOrder.UpdatedBy = updatedBy;
        workOrder.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workOrder;
    }

    public async Task<bool> DeleteWorkOrderAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdAsync(id, cancellationToken);
        if (workOrder == null)
        {
            return false;
        }

        workOrder.IsDeleted = true;
        workOrder.DeletedAt = DateTime.UtcNow;
        workOrder.UpdatedBy = deletedBy;
        workOrder.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WorkOrder> SubmitWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        return await TransitionStatusAsync(id, WorkOrderStatus.Open, userId, notes, cancellationToken);
    }

    public async Task<WorkOrder> StartWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var workOrder = await TransitionStatusAsync(id, WorkOrderStatus.InProgress, userId, notes, cancellationToken);

        if (workOrder.ActualStartDate == null)
        {
            workOrder.ActualStartDate = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return workOrder;
    }

    public async Task<WorkOrder> CompleteWorkOrderAsync(int id, int userId, string? completionNotes = null, DateTime? actualEndDate = null, CancellationToken cancellationToken = default)
    {
        var workOrder = await TransitionStatusAsync(id, WorkOrderStatus.Completed, userId, "Work order completed", cancellationToken);

        workOrder.CompletionNotes = completionNotes;
        workOrder.ActualEndDate = actualEndDate ?? DateTime.UtcNow;

        // Calculate actual hours from labor entries
        var totalHours = await _unitOfWork.WorkOrderLabor.Query()
            .Where(l => l.WorkOrderId == id)
            .SumAsync(l => l.HoursWorked, cancellationToken);
        workOrder.ActualHours = totalHours;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update PM schedule if this was a PM-generated work order
        if (workOrder.PreventiveMaintenanceScheduleId.HasValue)
        {
            var schedule = await _unitOfWork.PreventiveMaintenanceSchedules.GetByIdAsync(
                workOrder.PreventiveMaintenanceScheduleId.Value, cancellationToken);
            if (schedule != null)
            {
                schedule.LastCompletedDate = workOrder.ActualEndDate;
                // Next due date calculation should be done by PM service
            }
        }

        return workOrder;
    }

    public async Task<WorkOrder> HoldWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        return await TransitionStatusAsync(id, WorkOrderStatus.OnHold, userId, notes, cancellationToken);
    }

    public async Task<WorkOrder> ResumeWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        return await TransitionStatusAsync(id, WorkOrderStatus.InProgress, userId, notes ?? "Work order resumed", cancellationToken);
    }

    public async Task<WorkOrder> CancelWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        return await TransitionStatusAsync(id, WorkOrderStatus.Cancelled, userId, notes ?? "Work order cancelled", cancellationToken);
    }

    public async Task<WorkOrder> ReopenWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        return await TransitionStatusAsync(id, WorkOrderStatus.Open, userId, notes ?? "Work order reopened", cancellationToken);
    }

    private async Task<WorkOrder> TransitionStatusAsync(int id, WorkOrderStatus toStatus, int userId, string? notes, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {id} not found");

        var fromStatus = workOrder.Status;

        // Validate transition
        if (!IsValidTransition(fromStatus, toStatus))
        {
            throw new InvalidOperationException($"Cannot transition from {fromStatus} to {toStatus}");
        }

        workOrder.Status = toStatus;
        workOrder.UpdatedAt = DateTime.UtcNow;
        workOrder.UpdatedBy = userId;

        // Add history entry
        var history = new WorkOrderHistory
        {
            WorkOrderId = id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedById = userId,
            ChangedAt = DateTime.UtcNow,
            Notes = notes
        };

        await _unitOfWork.WorkOrderHistory.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workOrder;
    }

    private static bool IsValidTransition(WorkOrderStatus from, WorkOrderStatus to)
    {
        return (from, to) switch
        {
            (WorkOrderStatus.Draft, WorkOrderStatus.Open) => true,
            (WorkOrderStatus.Open, WorkOrderStatus.InProgress) => true,
            (WorkOrderStatus.Open, WorkOrderStatus.Cancelled) => true,
            (WorkOrderStatus.InProgress, WorkOrderStatus.OnHold) => true,
            (WorkOrderStatus.InProgress, WorkOrderStatus.Completed) => true,
            (WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled) => true,
            (WorkOrderStatus.OnHold, WorkOrderStatus.InProgress) => true,
            (WorkOrderStatus.OnHold, WorkOrderStatus.Cancelled) => true,
            // Reopen transitions
            (WorkOrderStatus.Cancelled, WorkOrderStatus.Open) => true,
            (WorkOrderStatus.Completed, WorkOrderStatus.Open) => true,
            _ => false
        };
    }

    public async Task<IEnumerable<WorkOrderHistory>> GetWorkOrderHistoryAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrderHistory.Query()
            .Include(h => h.ChangedBy)
            .Where(h => h.WorkOrderId == workOrderId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkOrderComment>> GetWorkOrderCommentsAsync(int workOrderId, bool includeInternal, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.WorkOrderComments.Query()
            .Include(c => c.CreatedBy)
            .Where(c => c.WorkOrderId == workOrderId);

        if (!includeInternal)
        {
            query = query.Where(c => !c.IsInternal);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderComment> AddCommentAsync(int workOrderId, string comment, bool isInternal, int createdBy, CancellationToken cancellationToken = default)
    {
        var workOrderComment = new WorkOrderComment
        {
            WorkOrderId = workOrderId,
            Comment = comment,
            IsInternal = isInternal,
            CreatedById = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.WorkOrderComments.AddAsync(workOrderComment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workOrderComment;
    }

    public async Task<IEnumerable<WorkOrderLabor>> GetWorkOrderLaborAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrderLabor.Query()
            .Include(l => l.User)
            .Where(l => l.WorkOrderId == workOrderId)
            .OrderByDescending(l => l.WorkDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderLabor> AddLaborEntryAsync(WorkOrderLabor laborEntry, CancellationToken cancellationToken = default)
    {
        laborEntry.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.WorkOrderLabor.AddAsync(laborEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update actual hours on work order
        var totalHours = await _unitOfWork.WorkOrderLabor.Query()
            .Where(l => l.WorkOrderId == laborEntry.WorkOrderId)
            .SumAsync(l => l.HoursWorked, cancellationToken);

        var workOrder = await _unitOfWork.WorkOrders.GetByIdAsync(laborEntry.WorkOrderId, cancellationToken);
        if (workOrder != null)
        {
            workOrder.ActualHours = totalHours;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return laborEntry;
    }

    public async Task<bool> DeleteLaborEntryAsync(int workOrderId, int laborId, CancellationToken cancellationToken = default)
    {
        var laborEntry = await _unitOfWork.WorkOrderLabor.Query()
            .FirstOrDefaultAsync(l => l.Id == laborId && l.WorkOrderId == workOrderId, cancellationToken);

        if (laborEntry == null)
        {
            return false;
        }

        _unitOfWork.WorkOrderLabor.Remove(laborEntry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update actual hours on work order
        var totalHours = await _unitOfWork.WorkOrderLabor.Query()
            .Where(l => l.WorkOrderId == workOrderId)
            .SumAsync(l => l.HoursWorked, cancellationToken);

        var workOrder = await _unitOfWork.WorkOrders.GetByIdAsync(workOrderId, cancellationToken);
        if (workOrder != null)
        {
            workOrder.ActualHours = totalHours;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<WorkOrderLaborSummary> GetLaborSummaryAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var laborEntries = await _unitOfWork.WorkOrderLabor.Query()
            .Where(l => l.WorkOrderId == workOrderId)
            .ToListAsync(cancellationToken);

        var summary = new WorkOrderLaborSummary
        {
            TotalHours = laborEntries.Sum(l => l.HoursWorked),
            TotalCost = laborEntries.Sum(l => l.HoursWorked * (l.HourlyRate ?? 0)),
            HoursByType = laborEntries
                .GroupBy(l => l.LaborType.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(l => l.HoursWorked))
        };

        return summary;
    }

    public async Task<IEnumerable<AssetPart>> GetWorkOrderPartsAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AssetParts.Query()
            .Include(ap => ap.Part)
            .Include(ap => ap.Asset)
            .Where(ap => ap.WorkOrderId == workOrderId)
            .OrderByDescending(ap => ap.UsedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var workOrders = await _unitOfWork.WorkOrders.Query().ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var weekFromNow = now.AddDays(7);

        var dashboard = new WorkOrderDashboard
        {
            TotalCount = workOrders.Count,
            ByStatus = workOrders
                .GroupBy(w => w.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ByType = workOrders
                .GroupBy(w => w.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ByPriority = workOrders
                .GroupBy(w => w.Priority.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            OverdueCount = workOrders
                .Count(w => w.ScheduledEndDate < now &&
                           w.Status != WorkOrderStatus.Completed &&
                           w.Status != WorkOrderStatus.Cancelled),
            DueThisWeekCount = workOrders
                .Count(w => w.ScheduledEndDate >= now &&
                           w.ScheduledEndDate <= weekFromNow &&
                           w.Status != WorkOrderStatus.Completed &&
                           w.Status != WorkOrderStatus.Cancelled)
        };

        return dashboard;
    }

    public async Task<string> GenerateWorkOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"WO-{today:yyyyMMdd}";

        // Get the highest existing number for today
        var lastWorkOrder = await _unitOfWork.WorkOrders.Query()
            .IgnoreQueryFilters()
            .Where(w => w.WorkOrderNumber.StartsWith(prefix))
            .OrderByDescending(w => w.WorkOrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (lastWorkOrder != null)
        {
            var parts = lastWorkOrder.WorkOrderNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{nextNumber:D4}";
    }
}
