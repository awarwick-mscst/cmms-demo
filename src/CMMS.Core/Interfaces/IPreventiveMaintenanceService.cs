using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IPreventiveMaintenanceService
{
    // CRUD operations
    Task<PagedResult<PreventiveMaintenanceSchedule>> GetSchedulesAsync(PreventiveMaintenanceScheduleFilter filter, CancellationToken cancellationToken = default);
    Task<PreventiveMaintenanceSchedule?> GetScheduleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PreventiveMaintenanceSchedule> CreateScheduleAsync(PreventiveMaintenanceSchedule schedule, int createdBy, CancellationToken cancellationToken = default);
    Task<PreventiveMaintenanceSchedule> UpdateScheduleAsync(PreventiveMaintenanceSchedule schedule, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteScheduleAsync(int id, int deletedBy, CancellationToken cancellationToken = default);

    // Work order generation
    Task<GenerateWorkOrdersResult> GenerateDueWorkOrdersAsync(int createdBy, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GenerateWorkOrderForScheduleAsync(int scheduleId, int createdBy, CancellationToken cancellationToken = default);

    // Upcoming maintenance
    Task<IEnumerable<UpcomingMaintenance>> GetUpcomingMaintenanceAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    // Schedule helpers
    DateTime CalculateNextDueDate(PreventiveMaintenanceSchedule schedule, DateTime? fromDate = null);
    Task UpdateScheduleAfterCompletionAsync(int scheduleId, DateTime completedDate, CancellationToken cancellationToken = default);
}

public class PreventiveMaintenanceScheduleFilter
{
    public string? Search { get; set; }
    public int? AssetId { get; set; }
    public string? FrequencyType { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? DueBefore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public class UpcomingMaintenance
{
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
    public string Priority { get; set; } = string.Empty;
}

public class GenerateWorkOrdersResult
{
    public int SchedulesProcessed { get; set; }
    public int WorkOrdersCreated { get; set; }
    public List<int> CreatedWorkOrderIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
