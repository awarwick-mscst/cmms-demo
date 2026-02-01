using CMMS.Core.Entities;
using CMMS.Core.Enums;

namespace CMMS.Core.Interfaces;

public interface IWorkOrderService
{
    // CRUD operations
    Task<PagedResult<WorkOrder>> GetWorkOrdersAsync(WorkOrderFilter filter, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetWorkOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetWorkOrderByNumberAsync(string workOrderNumber, CancellationToken cancellationToken = default);
    Task<WorkOrder> CreateWorkOrderAsync(WorkOrder workOrder, int createdBy, CancellationToken cancellationToken = default);
    Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteWorkOrderAsync(int id, int deletedBy, CancellationToken cancellationToken = default);

    // Status transitions
    Task<WorkOrder> SubmitWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default);
    Task<WorkOrder> StartWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default);
    Task<WorkOrder> CompleteWorkOrderAsync(int id, int userId, string? completionNotes = null, DateTime? actualEndDate = null, CancellationToken cancellationToken = default);
    Task<WorkOrder> HoldWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default);
    Task<WorkOrder> ResumeWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default);
    Task<WorkOrder> CancelWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default);
    Task<WorkOrder> ReopenWorkOrderAsync(int id, int userId, string? notes = null, CancellationToken cancellationToken = default);

    // History
    Task<IEnumerable<WorkOrderHistory>> GetWorkOrderHistoryAsync(int workOrderId, CancellationToken cancellationToken = default);

    // Comments
    Task<IEnumerable<WorkOrderComment>> GetWorkOrderCommentsAsync(int workOrderId, bool includeInternal, CancellationToken cancellationToken = default);
    Task<WorkOrderComment> AddCommentAsync(int workOrderId, string comment, bool isInternal, int createdBy, CancellationToken cancellationToken = default);

    // Labor
    Task<IEnumerable<WorkOrderLabor>> GetWorkOrderLaborAsync(int workOrderId, CancellationToken cancellationToken = default);
    Task<WorkOrderLabor> AddLaborEntryAsync(WorkOrderLabor laborEntry, CancellationToken cancellationToken = default);
    Task<bool> DeleteLaborEntryAsync(int workOrderId, int laborId, CancellationToken cancellationToken = default);
    Task<WorkOrderLaborSummary> GetLaborSummaryAsync(int workOrderId, CancellationToken cancellationToken = default);

    // Parts (via AssetPart)
    Task<IEnumerable<AssetPart>> GetWorkOrderPartsAsync(int workOrderId, CancellationToken cancellationToken = default);

    // Dashboard
    Task<WorkOrderDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);

    // Work order number generation
    Task<string> GenerateWorkOrderNumberAsync(CancellationToken cancellationToken = default);
}

public class WorkOrderFilter
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? AssetId { get; set; }
    public int? LocationId { get; set; }
    public int? AssignedToId { get; set; }
    public DateTime? ScheduledStartFrom { get; set; }
    public DateTime? ScheduledStartTo { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public class WorkOrderLaborSummary
{
    public decimal TotalHours { get; set; }
    public decimal TotalCost { get; set; }
    public Dictionary<string, decimal> HoursByType { get; set; } = new();
}

public class WorkOrderDashboard
{
    public int TotalCount { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public int OverdueCount { get; set; }
    public int DueThisWeekCount { get; set; }
}
