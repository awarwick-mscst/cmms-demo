using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class PreventiveMaintenanceService : IPreventiveMaintenanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkOrderService _workOrderService;

    public PreventiveMaintenanceService(IUnitOfWork unitOfWork, IWorkOrderService workOrderService)
    {
        _unitOfWork = unitOfWork;
        _workOrderService = workOrderService;
    }

    public async Task<PagedResult<PreventiveMaintenanceSchedule>> GetSchedulesAsync(
        PreventiveMaintenanceScheduleFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(p => p.Asset)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.WorkOrderTitle.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        if (filter.AssetId.HasValue)
        {
            query = query.Where(p => p.AssetId == filter.AssetId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.FrequencyType) && Enum.TryParse<FrequencyType>(filter.FrequencyType, out var freqType))
        {
            query = query.Where(p => p.FrequencyType == freqType);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }

        if (filter.DueBefore.HasValue)
        {
            query = query.Where(p => p.NextDueDate <= filter.DueBefore.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "nextduedate" => filter.SortDescending ? query.OrderByDescending(p => p.NextDueDate) : query.OrderBy(p => p.NextDueDate),
            "frequency" => filter.SortDescending ? query.OrderByDescending(p => p.FrequencyType) : query.OrderBy(p => p.FrequencyType),
            "createdat" => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderBy(p => p.NextDueDate)
        };

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PreventiveMaintenanceSchedule>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PreventiveMaintenanceSchedule?> GetScheduleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(p => p.Asset)
            .Include(p => p.GeneratedWorkOrders.Where(w => !w.IsDeleted).OrderByDescending(w => w.CreatedAt).Take(10))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PreventiveMaintenanceSchedule> CreateScheduleAsync(
        PreventiveMaintenanceSchedule schedule,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        schedule.CreatedBy = createdBy;
        schedule.CreatedAt = DateTime.UtcNow;

        // Calculate initial next due date if not set
        if (schedule.NextDueDate == null)
        {
            schedule.NextDueDate = CalculateNextDueDate(schedule);
        }

        await _unitOfWork.PreventiveMaintenanceSchedules.AddAsync(schedule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return schedule;
    }

    public async Task<PreventiveMaintenanceSchedule> UpdateScheduleAsync(
        PreventiveMaintenanceSchedule schedule,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        schedule.UpdatedBy = updatedBy;
        schedule.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.PreventiveMaintenanceSchedules.Update(schedule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return schedule;
    }

    public async Task<bool> DeleteScheduleAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.PreventiveMaintenanceSchedules.GetByIdAsync(id, cancellationToken);
        if (schedule == null)
        {
            return false;
        }

        schedule.IsDeleted = true;
        schedule.DeletedAt = DateTime.UtcNow;
        schedule.UpdatedBy = deletedBy;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<GenerateWorkOrdersResult> GenerateDueWorkOrdersAsync(int createdBy, CancellationToken cancellationToken = default)
    {
        var result = new GenerateWorkOrdersResult();
        var now = DateTime.UtcNow;

        // Get all active schedules that are due (including lead time)
        var dueSchedules = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(p => p.Asset)
            .Where(p => p.IsActive &&
                       p.NextDueDate != null &&
                       p.NextDueDate.Value.AddDays(-p.LeadTimeDays) <= now)
            .ToListAsync(cancellationToken);

        result.SchedulesProcessed = dueSchedules.Count;

        foreach (var schedule in dueSchedules)
        {
            try
            {
                // Check if work order already exists for this schedule and due date
                var existingWo = await _unitOfWork.WorkOrders.Query()
                    .Where(w => w.PreventiveMaintenanceScheduleId == schedule.Id &&
                               w.Status != WorkOrderStatus.Completed &&
                               w.Status != WorkOrderStatus.Cancelled)
                    .AnyAsync(cancellationToken);

                if (existingWo)
                {
                    continue; // Skip if open work order already exists
                }

                var workOrder = await GenerateWorkOrderForScheduleAsync(schedule.Id, createdBy, cancellationToken);
                if (workOrder != null)
                {
                    result.WorkOrdersCreated++;
                    result.CreatedWorkOrderIds.Add(workOrder.Id);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to generate work order for schedule {schedule.Id}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<WorkOrder?> GenerateWorkOrderForScheduleAsync(int scheduleId, int createdBy, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(p => p.Asset)
            .FirstOrDefaultAsync(p => p.Id == scheduleId, cancellationToken);

        if (schedule == null || !schedule.IsActive)
        {
            return null;
        }

        var workOrder = new WorkOrder
        {
            Type = WorkOrderType.PreventiveMaintenance,
            Priority = schedule.Priority,
            Status = WorkOrderStatus.Open,
            Title = schedule.WorkOrderTitle,
            Description = schedule.WorkOrderDescription,
            AssetId = schedule.AssetId,
            ScheduledStartDate = schedule.NextDueDate,
            ScheduledEndDate = schedule.NextDueDate,
            EstimatedHours = schedule.EstimatedHours,
            PreventiveMaintenanceScheduleId = schedule.Id
        };

        var created = await _workOrderService.CreateWorkOrderAsync(workOrder, createdBy, cancellationToken);

        // Submit the work order to Open status
        await _workOrderService.SubmitWorkOrderAsync(created.Id, createdBy, "Auto-generated from PM schedule", cancellationToken);

        // Calculate and set next due date
        schedule.NextDueDate = CalculateNextDueDate(schedule, schedule.NextDueDate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created;
    }

    public async Task<IEnumerable<UpcomingMaintenance>> GetUpcomingMaintenanceAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        var now = DateTime.UtcNow;

        var schedules = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(p => p.Asset)
            .Where(p => p.IsActive &&
                       p.NextDueDate != null &&
                       p.NextDueDate <= cutoffDate)
            .OrderBy(p => p.NextDueDate)
            .ToListAsync(cancellationToken);

        return schedules.Select(s => new UpcomingMaintenance
        {
            ScheduleId = s.Id,
            ScheduleName = s.Name,
            AssetId = s.AssetId,
            AssetName = s.Asset?.Name,
            DueDate = s.NextDueDate!.Value,
            DaysUntilDue = (int)(s.NextDueDate.Value - now).TotalDays,
            Priority = s.Priority.ToString()
        });
    }

    public DateTime CalculateNextDueDate(PreventiveMaintenanceSchedule schedule, DateTime? fromDate = null)
    {
        var baseDate = fromDate ?? DateTime.UtcNow;

        return schedule.FrequencyType switch
        {
            FrequencyType.Daily => baseDate.AddDays(schedule.FrequencyValue),
            FrequencyType.Weekly => CalculateWeeklyDueDate(baseDate, schedule.FrequencyValue, schedule.DayOfWeek),
            FrequencyType.BiWeekly => baseDate.AddDays(14 * schedule.FrequencyValue),
            FrequencyType.Monthly => CalculateMonthlyDueDate(baseDate, schedule.FrequencyValue, schedule.DayOfMonth),
            FrequencyType.Quarterly => baseDate.AddMonths(3 * schedule.FrequencyValue),
            FrequencyType.SemiAnnually => baseDate.AddMonths(6 * schedule.FrequencyValue),
            FrequencyType.Annually => baseDate.AddYears(schedule.FrequencyValue),
            FrequencyType.Custom => baseDate.AddDays(schedule.FrequencyValue), // Custom uses frequency value as days
            _ => baseDate.AddMonths(1)
        };
    }

    private static DateTime CalculateWeeklyDueDate(DateTime baseDate, int frequencyValue, int? dayOfWeek)
    {
        var nextDate = baseDate.AddDays(7 * frequencyValue);

        if (dayOfWeek.HasValue && dayOfWeek.Value >= 0 && dayOfWeek.Value <= 6)
        {
            var targetDay = (DayOfWeek)dayOfWeek.Value;
            var daysUntilTarget = ((int)targetDay - (int)nextDate.DayOfWeek + 7) % 7;
            nextDate = nextDate.AddDays(daysUntilTarget);
        }

        return nextDate;
    }

    private static DateTime CalculateMonthlyDueDate(DateTime baseDate, int frequencyValue, int? dayOfMonth)
    {
        var nextDate = baseDate.AddMonths(frequencyValue);

        if (dayOfMonth.HasValue && dayOfMonth.Value >= 1 && dayOfMonth.Value <= 31)
        {
            var daysInMonth = DateTime.DaysInMonth(nextDate.Year, nextDate.Month);
            var targetDay = Math.Min(dayOfMonth.Value, daysInMonth);
            nextDate = new DateTime(nextDate.Year, nextDate.Month, targetDay, nextDate.Hour, nextDate.Minute, nextDate.Second);
        }

        return nextDate;
    }

    public async Task UpdateScheduleAfterCompletionAsync(int scheduleId, DateTime completedDate, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.PreventiveMaintenanceSchedules.GetByIdAsync(scheduleId, cancellationToken);
        if (schedule == null)
        {
            return;
        }

        schedule.LastCompletedDate = completedDate;
        schedule.NextDueDate = CalculateNextDueDate(schedule, completedDate);
        schedule.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
