using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IWorkOrderTaskTemplateService
{
    // CRUD
    Task<PagedResult<WorkOrderTaskTemplate>> GetTemplatesAsync(WorkOrderTaskTemplateFilter filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkOrderTaskTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<WorkOrderTaskTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkOrderTaskTemplate> CreateTemplateAsync(WorkOrderTaskTemplate template, IEnumerable<WorkOrderTaskTemplateItem> items, int createdBy, CancellationToken cancellationToken = default);
    Task<WorkOrderTaskTemplate> UpdateTemplateAsync(WorkOrderTaskTemplate template, IEnumerable<WorkOrderTaskTemplateItem> items, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteTemplateAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
}

public class WorkOrderTaskTemplateFilter
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}
