using CMMS.Core.Entities;
using CMMS.Core.Enums;

namespace CMMS.Core.Interfaces;

public interface IPartService
{
    // Part CRUD
    Task<PagedResult<Part>> GetPartsAsync(PartFilter filter, CancellationToken cancellationToken = default);
    Task<Part?> GetPartByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Part?> GetPartByNumberAsync(string partNumber, CancellationToken cancellationToken = default);
    Task<Part> CreatePartAsync(Part part, int createdBy, CancellationToken cancellationToken = default);
    Task<Part> UpdatePartAsync(Part part, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeletePartAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
    Task<string> GeneratePartNumberAsync(int? categoryId, CancellationToken cancellationToken = default);
    Task<bool> PartNumberExistsAsync(string partNumber, int? excludeId = null, CancellationToken cancellationToken = default);

    // Stock management
    Task<IEnumerable<PartStock>> GetPartStocksAsync(int partId, CancellationToken cancellationToken = default);
    Task<PartStock?> GetPartStockAsync(int partId, int locationId, CancellationToken cancellationToken = default);
    Task<PartStock> AdjustStockAsync(int partId, int locationId, TransactionType transactionType, int quantity, decimal? unitCost, string? referenceType, int? referenceId, string? notes, int userId, CancellationToken cancellationToken = default);
    Task<(PartStock from, PartStock to)> TransferStockAsync(int partId, int fromLocationId, int toLocationId, int quantity, string? notes, int userId, CancellationToken cancellationToken = default);
    Task<PartStock> ReserveStockAsync(int partId, int locationId, int quantity, string? referenceType, int? referenceId, string? notes, int userId, CancellationToken cancellationToken = default);
    Task<PartStock> UnreserveStockAsync(int partId, int locationId, int quantity, string? referenceType, int? referenceId, string? notes, int userId, CancellationToken cancellationToken = default);

    // Transaction history
    Task<PagedResult<PartTransaction>> GetPartTransactionsAsync(PartTransactionFilter filter, CancellationToken cancellationToken = default);

    // Low stock alerts
    Task<IEnumerable<Part>> GetLowStockPartsAsync(CancellationToken cancellationToken = default);
    Task<ReorderStatus> GetReorderStatusAsync(int partId, CancellationToken cancellationToken = default);

    // Asset parts
    Task<AssetPart> UsePartOnAssetAsync(int assetId, int partId, int locationId, int quantity, decimal? unitCostOverride, int? workOrderId, string? notes, int userId, CancellationToken cancellationToken = default);
    Task<PagedResult<AssetPart>> GetAssetPartsAsync(AssetPartFilter filter, CancellationToken cancellationToken = default);
}

public class PartFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public string? Status { get; set; }
    public bool? LowStock { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public class PartTransactionFilter
{
    public int? PartId { get; set; }
    public int? LocationId { get; set; }
    public string? TransactionType { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class AssetPartFilter
{
    public int? AssetId { get; set; }
    public int? PartId { get; set; }
    public int? WorkOrderId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
