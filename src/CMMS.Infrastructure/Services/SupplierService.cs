using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<Supplier>> GetSuppliersAsync(SupplierFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Suppliers.Query()
            .Include(s => s.Parts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(search) ||
                (s.Code != null && s.Code.ToLower().Contains(search)) ||
                (s.ContactName != null && s.ContactName.ToLower().Contains(search)) ||
                (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "code" => filter.SortDescending ? query.OrderByDescending(s => s.Code) : query.OrderBy(s => s.Code),
            "createdat" => filter.SortDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Name)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Supplier>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Suppliers.Query()
            .Include(s => s.Parts)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Supplier?> GetSupplierByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Suppliers.Query()
            .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier, int createdBy, CancellationToken cancellationToken = default)
    {
        supplier.CreatedBy = createdBy;
        supplier.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Suppliers.AddAsync(supplier, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier;
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier, int updatedBy, CancellationToken cancellationToken = default)
    {
        supplier.UpdatedBy = updatedBy;
        supplier.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Suppliers.Update(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier;
    }

    public async Task<bool> DeleteSupplierAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
            return false;

        supplier.IsDeleted = true;
        supplier.DeletedAt = DateTime.UtcNow;
        supplier.UpdatedBy = deletedBy;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Suppliers.Query()
            .Where(s => s.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
