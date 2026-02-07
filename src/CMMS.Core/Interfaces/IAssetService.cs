using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IAssetService
{
    Task<PagedResult<Asset>> GetAssetsAsync(AssetFilter filter, CancellationToken cancellationToken = default);
    Task<Asset?> GetAssetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Asset?> GetAssetByTagAsync(string assetTag, CancellationToken cancellationToken = default);
    Task<Asset?> GetAssetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<Asset> CreateAssetAsync(Asset asset, int createdBy, CancellationToken cancellationToken = default);
    Task<Asset> UpdateAssetAsync(Asset asset, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssetAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
    Task<string> GenerateAssetTagAsync(int categoryId, CancellationToken cancellationToken = default);
}

public interface IAssetCategoryService
{
    Task<IEnumerable<AssetCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<AssetCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AssetCategory> CreateCategoryAsync(AssetCategory category, int createdBy, CancellationToken cancellationToken = default);
    Task<AssetCategory> UpdateCategoryAsync(AssetCategory category, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
}

public interface IAssetLocationService
{
    Task<IEnumerable<AssetLocation>> GetLocationsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<AssetLocation>> GetLocationTreeAsync(CancellationToken cancellationToken = default);
    Task<AssetLocation?> GetLocationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AssetLocation> CreateLocationAsync(AssetLocation location, int createdBy, CancellationToken cancellationToken = default);
    Task<AssetLocation> UpdateLocationAsync(AssetLocation location, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteLocationAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
}

public class AssetFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }
    public string? Status { get; set; }
    public string? Criticality { get; set; }
    public int? AssignedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
