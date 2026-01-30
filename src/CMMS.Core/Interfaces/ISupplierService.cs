using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface ISupplierService
{
    Task<PagedResult<Supplier>> GetSuppliersAsync(SupplierFilter filter, CancellationToken cancellationToken = default);
    Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Supplier?> GetSupplierByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Supplier> CreateSupplierAsync(Supplier supplier, int createdBy, CancellationToken cancellationToken = default);
    Task<Supplier> UpdateSupplierAsync(Supplier supplier, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteSupplierAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}

public class SupplierFilter
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}
