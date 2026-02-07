using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class AssetService : IAssetService
{
    private readonly IUnitOfWork _unitOfWork;

    public AssetService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<Asset>> GetAssetsAsync(AssetFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Assets.Query()
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.AssignedUser)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(search) ||
                a.AssetTag.ToLower().Contains(search) ||
                (a.Description != null && a.Description.ToLower().Contains(search)) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(search)) ||
                (a.Barcode != null && a.Barcode.ToLower().Contains(search)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == filter.CategoryId.Value);
        }

        if (filter.LocationId.HasValue)
        {
            query = query.Where(a => a.LocationId == filter.LocationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<AssetStatus>(filter.Status, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Criticality) && Enum.TryParse<AssetCriticality>(filter.Criticality, out var criticality))
        {
            query = query.Where(a => a.Criticality == criticality);
        }

        if (filter.AssignedTo.HasValue)
        {
            query = query.Where(a => a.AssignedTo == filter.AssignedTo.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
            "assettag" => filter.SortDescending ? query.OrderByDescending(a => a.AssetTag) : query.OrderBy(a => a.AssetTag),
            "status" => filter.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            "category" => filter.SortDescending ? query.OrderByDescending(a => a.Category.Name) : query.OrderBy(a => a.Category.Name),
            "createdat" => filter.SortDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Asset>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Asset?> GetAssetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Assets.Query()
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.AssignedUser)
            .Include(a => a.ParentAsset)
            .Include(a => a.ChildAssets)
            .Include(a => a.Documents.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Asset?> GetAssetByTagAsync(string assetTag, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Assets.Query()
            .Include(a => a.Category)
            .Include(a => a.Location)
            .FirstOrDefaultAsync(a => a.AssetTag == assetTag, cancellationToken);
    }

    public async Task<Asset?> GetAssetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Assets.Query()
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(a => a.Barcode == barcode, cancellationToken);
    }

    public async Task<Asset> CreateAssetAsync(Asset asset, int createdBy, CancellationToken cancellationToken = default)
    {
        asset.CreatedBy = createdBy;
        asset.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(asset.AssetTag))
        {
            asset.AssetTag = await GenerateAssetTagAsync(asset.CategoryId, cancellationToken);
        }

        await _unitOfWork.Assets.AddAsync(asset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return asset;
    }

    public async Task<Asset> UpdateAssetAsync(Asset asset, int updatedBy, CancellationToken cancellationToken = default)
    {
        asset.UpdatedBy = updatedBy;
        asset.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Assets.Update(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return asset;
    }

    public async Task<bool> DeleteAssetAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(id, cancellationToken);
        if (asset == null)
        {
            return false;
        }

        asset.IsDeleted = true;
        asset.DeletedAt = DateTime.UtcNow;
        asset.UpdatedBy = deletedBy;
        asset.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string> GenerateAssetTagAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.AssetCategories.GetByIdAsync(categoryId, cancellationToken);
        var prefix = category?.Code ?? "AST";

        // Get the highest existing number for this prefix
        var lastAsset = await _unitOfWork.Assets.Query()
            .IgnoreQueryFilters()
            .Where(a => a.AssetTag.StartsWith(prefix + "-"))
            .OrderByDescending(a => a.AssetTag)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (lastAsset != null)
        {
            var parts = lastAsset.AssetTag.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{nextNumber:D6}";
    }
}

public class AssetCategoryService : IAssetCategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public AssetCategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AssetCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.AssetCategories.Query()
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name);

        if (!includeInactive)
        {
            query = (IOrderedQueryable<AssetCategory>)query.Where(c => c.IsActive);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<AssetCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AssetCategories.Query()
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<AssetCategory> CreateCategoryAsync(AssetCategory category, int createdBy, CancellationToken cancellationToken = default)
    {
        category.CreatedBy = createdBy;
        category.CreatedAt = DateTime.UtcNow;

        if (category.ParentId.HasValue)
        {
            var parent = await _unitOfWork.AssetCategories.GetByIdAsync(category.ParentId.Value, cancellationToken);
            category.Level = (parent?.Level ?? 0) + 1;
        }

        await _unitOfWork.AssetCategories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<AssetCategory> UpdateCategoryAsync(AssetCategory category, int updatedBy, CancellationToken cancellationToken = default)
    {
        category.UpdatedBy = updatedBy;
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AssetCategories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.AssetCategories.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return false;
        }

        // Check if category has assets
        var hasAssets = await _unitOfWork.Assets.AnyAsync(a => a.CategoryId == id, cancellationToken);
        if (hasAssets)
        {
            return false; // Cannot delete category with assets
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedBy = deletedBy;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class AssetLocationService : IAssetLocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public AssetLocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AssetLocation>> GetLocationsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.AssetLocations.Query()
            .Include(l => l.Parent)
            .OrderBy(l => l.FullPath);

        if (!includeInactive)
        {
            query = (IOrderedQueryable<AssetLocation>)query.Where(l => l.IsActive);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AssetLocation>> GetLocationTreeAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AssetLocations.Query()
            .Include(l => l.Children)
            .Where(l => l.ParentId == null && l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AssetLocation?> GetLocationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AssetLocations.Query()
            .Include(l => l.Parent)
            .Include(l => l.Children)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<AssetLocation> CreateLocationAsync(AssetLocation location, int createdBy, CancellationToken cancellationToken = default)
    {
        location.CreatedBy = createdBy;
        location.CreatedAt = DateTime.UtcNow;

        if (location.ParentId.HasValue)
        {
            var parent = await _unitOfWork.AssetLocations.GetByIdAsync(location.ParentId.Value, cancellationToken);
            location.Level = (parent?.Level ?? 0) + 1;
            location.FullPath = parent != null
                ? $"{parent.FullPath} > {location.Name}"
                : location.Name;
        }
        else
        {
            location.Level = 0;
            location.FullPath = location.Name;
        }

        await _unitOfWork.AssetLocations.AddAsync(location, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return location;
    }

    public async Task<AssetLocation> UpdateLocationAsync(AssetLocation location, int updatedBy, CancellationToken cancellationToken = default)
    {
        location.UpdatedBy = updatedBy;
        location.UpdatedAt = DateTime.UtcNow;

        // Update full path if parent changed
        if (location.ParentId.HasValue)
        {
            var parent = await _unitOfWork.AssetLocations.GetByIdAsync(location.ParentId.Value, cancellationToken);
            location.FullPath = parent != null
                ? $"{parent.FullPath} > {location.Name}"
                : location.Name;
        }
        else
        {
            location.FullPath = location.Name;
        }

        _unitOfWork.AssetLocations.Update(location);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return location;
    }

    public async Task<bool> DeleteLocationAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var location = await _unitOfWork.AssetLocations.GetByIdAsync(id, cancellationToken);
        if (location == null)
        {
            return false;
        }

        // Check if location has assets
        var hasAssets = await _unitOfWork.Assets.AnyAsync(a => a.LocationId == id, cancellationToken);
        if (hasAssets)
        {
            return false;
        }

        location.IsDeleted = true;
        location.DeletedAt = DateTime.UtcNow;
        location.UpdatedBy = deletedBy;
        location.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
