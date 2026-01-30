using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class StorageLocationService : IStorageLocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public StorageLocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<StorageLocation>> GetLocationsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.StorageLocations.Query()
            .Include(l => l.PartStocks)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        return await query
            .OrderBy(l => l.FullPath ?? l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StorageLocation>> GetLocationTreeAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.StorageLocations.Query()
            .Include(l => l.Children)
            .Include(l => l.PartStocks)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        var locations = await query.ToListAsync(cancellationToken);

        return locations
            .Where(l => l.ParentId == null)
            .OrderBy(l => l.Name)
            .ToList();
    }

    public async Task<StorageLocation?> GetLocationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.StorageLocations.Query()
            .Include(l => l.Parent)
            .Include(l => l.Children)
            .Include(l => l.PartStocks)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<StorageLocation?> GetLocationByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.StorageLocations.Query()
            .FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
    }

    public async Task<StorageLocation> CreateLocationAsync(StorageLocation location, int createdBy, CancellationToken cancellationToken = default)
    {
        location.CreatedBy = createdBy;
        location.CreatedAt = DateTime.UtcNow;

        if (location.ParentId.HasValue)
        {
            var parent = await _unitOfWork.StorageLocations.GetByIdAsync(location.ParentId.Value, cancellationToken);
            location.Level = (parent?.Level ?? 0) + 1;
        }
        else
        {
            location.Level = 0;
        }

        location.FullPath = await BuildFullPathAsync(location.ParentId, location.Name, cancellationToken);

        await _unitOfWork.StorageLocations.AddAsync(location, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return location;
    }

    public async Task<StorageLocation> UpdateLocationAsync(StorageLocation location, int updatedBy, CancellationToken cancellationToken = default)
    {
        location.UpdatedBy = updatedBy;
        location.UpdatedAt = DateTime.UtcNow;

        if (location.ParentId.HasValue)
        {
            var parent = await _unitOfWork.StorageLocations.GetByIdAsync(location.ParentId.Value, cancellationToken);
            location.Level = (parent?.Level ?? 0) + 1;
        }
        else
        {
            location.Level = 0;
        }

        location.FullPath = await BuildFullPathAsync(location.ParentId, location.Name, cancellationToken);

        _unitOfWork.StorageLocations.Update(location);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return location;
    }

    public async Task<bool> DeleteLocationAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var location = await _unitOfWork.StorageLocations.GetByIdAsync(id, cancellationToken);
        if (location == null)
            return false;

        location.IsDeleted = true;
        location.DeletedAt = DateTime.UtcNow;
        location.UpdatedBy = deletedBy;
        location.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.StorageLocations.Query()
            .Where(l => l.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(l => l.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasStockAsync(int locationId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.PartStocks.Query()
            .AnyAsync(ps => ps.LocationId == locationId && ps.QuantityOnHand > 0, cancellationToken);
    }

    public async Task<string> BuildFullPathAsync(int? parentId, string name, CancellationToken cancellationToken = default)
    {
        if (!parentId.HasValue)
        {
            return name;
        }

        var pathParts = new List<string> { name };
        var currentParentId = parentId;

        while (currentParentId.HasValue)
        {
            var parent = await _unitOfWork.StorageLocations.GetByIdAsync(currentParentId.Value, cancellationToken);
            if (parent == null)
                break;

            pathParts.Insert(0, parent.Name);
            currentParentId = parent.ParentId;
        }

        return string.Join(" > ", pathParts);
    }
}
