using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class PartService : IPartService
{
    private readonly IUnitOfWork _unitOfWork;

    public PartService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Part CRUD

    public async Task<PagedResult<Part>> GetPartsAsync(PartFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Parts.Query()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Stocks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.PartNumber.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(search)) ||
                (p.Manufacturer != null && p.Manufacturer.ToLower().Contains(search)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == filter.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<PartStatus>(filter.Status, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (filter.LowStock == true)
        {
            query = query.Where(p => p.Stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved) <= p.ReorderPoint);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "partnumber" => filter.SortDescending ? query.OrderByDescending(p => p.PartNumber) : query.OrderBy(p => p.PartNumber),
            "category" => filter.SortDescending ? query.OrderByDescending(p => p.Category!.Name) : query.OrderBy(p => p.Category!.Name),
            "unitcost" => filter.SortDescending ? query.OrderByDescending(p => p.UnitCost) : query.OrderBy(p => p.UnitCost),
            "createdat" => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Part>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Part?> GetPartByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Parts.Query()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Stocks)
                .ThenInclude(s => s.Location)
            .Include(p => p.Stocks)
                .ThenInclude(s => s.LastCountByUser)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Part?> GetPartByNumberAsync(string partNumber, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Parts.Query()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.PartNumber == partNumber, cancellationToken);
    }

    public async Task<Part?> GetPartByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Parts.Query()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Stocks)
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);
    }

    public async Task<Part> CreatePartAsync(Part part, int createdBy, CancellationToken cancellationToken = default)
    {
        part.CreatedBy = createdBy;
        part.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(part.PartNumber))
        {
            part.PartNumber = await GeneratePartNumberAsync(part.CategoryId, cancellationToken);
        }

        await _unitOfWork.Parts.AddAsync(part, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return part;
    }

    public async Task<Part> UpdatePartAsync(Part part, int updatedBy, CancellationToken cancellationToken = default)
    {
        part.UpdatedBy = updatedBy;
        part.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Parts.Update(part);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return part;
    }

    public async Task<bool> DeletePartAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var part = await _unitOfWork.Parts.GetByIdAsync(id, cancellationToken);
        if (part == null)
            return false;

        part.IsDeleted = true;
        part.DeletedAt = DateTime.UtcNow;
        part.UpdatedBy = deletedBy;
        part.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string> GeneratePartNumberAsync(int? categoryId, CancellationToken cancellationToken = default)
    {
        string prefix = "PRT";

        if (categoryId.HasValue)
        {
            var category = await _unitOfWork.PartCategories.GetByIdAsync(categoryId.Value, cancellationToken);
            if (category?.Code != null)
            {
                prefix = category.Code;
            }
        }

        var lastPart = await _unitOfWork.Parts.Query()
            .IgnoreQueryFilters()
            .Where(p => p.PartNumber.StartsWith(prefix + "-"))
            .OrderByDescending(p => p.PartNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (lastPart != null)
        {
            var parts = lastPart.PartNumber.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{nextNumber:D6}";
    }

    public async Task<bool> PartNumberExistsAsync(string partNumber, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Parts.Query()
            .Where(p => p.PartNumber == partNumber);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    #endregion

    #region Stock Management

    public async Task<IEnumerable<PartStock>> GetPartStocksAsync(int partId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.PartStocks.Query()
            .Include(ps => ps.Location)
            .Include(ps => ps.LastCountByUser)
            .Where(ps => ps.PartId == partId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PartStock?> GetPartStockAsync(int partId, int locationId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.PartStocks.Query()
            .Include(ps => ps.Location)
            .FirstOrDefaultAsync(ps => ps.PartId == partId && ps.LocationId == locationId, cancellationToken);
    }

    public async Task<PartStock> AdjustStockAsync(int partId, int locationId, TransactionType transactionType, int quantity, decimal? unitCost, string? referenceType, int? referenceId, string? notes, int userId, CancellationToken cancellationToken = default)
    {
        var part = await _unitOfWork.Parts.GetByIdAsync(partId, cancellationToken);
        if (part == null)
            throw new InvalidOperationException("Part not found");

        var stock = await GetOrCreateStockAsync(partId, locationId, userId, cancellationToken);

        var actualQuantity = transactionType switch
        {
            TransactionType.Receive => quantity,
            TransactionType.Issue => -quantity,
            TransactionType.Adjust => quantity,
            _ => throw new InvalidOperationException($"Invalid transaction type for adjustment: {transactionType}")
        };

        stock.QuantityOnHand += actualQuantity;
        stock.UpdatedBy = userId;
        stock.UpdatedAt = DateTime.UtcNow;

        if (stock.QuantityOnHand < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        var transaction = new PartTransaction
        {
            PartId = partId,
            LocationId = locationId,
            TransactionType = transactionType,
            Quantity = actualQuantity,
            UnitCost = unitCost ?? part.UnitCost,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PartTransactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stock;
    }

    public async Task<(PartStock from, PartStock to)> TransferStockAsync(int partId, int fromLocationId, int toLocationId, int quantity, string? notes, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Transfer quantity must be positive");

        var fromStock = await GetPartStockAsync(partId, fromLocationId, cancellationToken);
        if (fromStock == null || fromStock.QuantityAvailable < quantity)
            throw new InvalidOperationException("Insufficient available stock for transfer");

        var part = await _unitOfWork.Parts.GetByIdAsync(partId, cancellationToken);
        if (part == null)
            throw new InvalidOperationException("Part not found");

        var toStock = await GetOrCreateStockAsync(partId, toLocationId, userId, cancellationToken);

        fromStock.QuantityOnHand -= quantity;
        fromStock.UpdatedBy = userId;
        fromStock.UpdatedAt = DateTime.UtcNow;

        toStock.QuantityOnHand += quantity;
        toStock.UpdatedBy = userId;
        toStock.UpdatedAt = DateTime.UtcNow;

        var transaction = new PartTransaction
        {
            PartId = partId,
            LocationId = fromLocationId,
            ToLocationId = toLocationId,
            TransactionType = TransactionType.Transfer,
            Quantity = quantity,
            UnitCost = part.UnitCost,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PartTransactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (fromStock, toStock);
    }

    public async Task<PartStock> ReserveStockAsync(int partId, int locationId, int quantity, string? referenceType, int? referenceId, string? notes, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Reserve quantity must be positive");

        var stock = await GetPartStockAsync(partId, locationId, cancellationToken);
        if (stock == null || stock.QuantityAvailable < quantity)
            throw new InvalidOperationException("Insufficient available stock for reservation");

        var part = await _unitOfWork.Parts.GetByIdAsync(partId, cancellationToken);
        if (part == null)
            throw new InvalidOperationException("Part not found");

        stock.QuantityReserved += quantity;
        stock.UpdatedBy = userId;
        stock.UpdatedAt = DateTime.UtcNow;

        var transaction = new PartTransaction
        {
            PartId = partId,
            LocationId = locationId,
            TransactionType = TransactionType.Reserve,
            Quantity = quantity,
            UnitCost = part.UnitCost,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PartTransactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stock;
    }

    public async Task<PartStock> UnreserveStockAsync(int partId, int locationId, int quantity, string? referenceType, int? referenceId, string? notes, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Unreserve quantity must be positive");

        var stock = await GetPartStockAsync(partId, locationId, cancellationToken);
        if (stock == null || stock.QuantityReserved < quantity)
            throw new InvalidOperationException("Insufficient reserved stock to unreserve");

        var part = await _unitOfWork.Parts.GetByIdAsync(partId, cancellationToken);
        if (part == null)
            throw new InvalidOperationException("Part not found");

        stock.QuantityReserved -= quantity;
        stock.UpdatedBy = userId;
        stock.UpdatedAt = DateTime.UtcNow;

        var transaction = new PartTransaction
        {
            PartId = partId,
            LocationId = locationId,
            TransactionType = TransactionType.Unreserve,
            Quantity = -quantity,
            UnitCost = part.UnitCost,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PartTransactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stock;
    }

    private async Task<PartStock> GetOrCreateStockAsync(int partId, int locationId, int userId, CancellationToken cancellationToken)
    {
        var stock = await GetPartStockAsync(partId, locationId, cancellationToken);
        if (stock != null)
            return stock;

        stock = new PartStock
        {
            PartId = partId,
            LocationId = locationId,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PartStocks.AddAsync(stock, cancellationToken);
        return stock;
    }

    #endregion

    #region Transaction History

    public async Task<PagedResult<PartTransaction>> GetPartTransactionsAsync(PartTransactionFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PartTransactions.Query()
            .Include(t => t.Part)
            .Include(t => t.Location)
            .Include(t => t.ToLocation)
            .Include(t => t.CreatedByUser)
            .AsQueryable();

        if (filter.PartId.HasValue)
        {
            query = query.Where(t => t.PartId == filter.PartId.Value);
        }

        if (filter.LocationId.HasValue)
        {
            query = query.Where(t => t.LocationId == filter.LocationId.Value || t.ToLocationId == filter.LocationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType) && Enum.TryParse<TransactionType>(filter.TransactionType, out var transType))
        {
            query = query.Where(t => t.TransactionType == transType);
        }

        if (!string.IsNullOrWhiteSpace(filter.ReferenceType))
        {
            query = query.Where(t => t.ReferenceType == filter.ReferenceType);
        }

        if (filter.ReferenceId.HasValue)
        {
            query = query.Where(t => t.ReferenceId == filter.ReferenceId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "part" => filter.SortDescending ? query.OrderByDescending(t => t.Part.Name) : query.OrderBy(t => t.Part.Name),
            "type" => filter.SortDescending ? query.OrderByDescending(t => t.TransactionType) : query.OrderBy(t => t.TransactionType),
            "quantity" => filter.SortDescending ? query.OrderByDescending(t => t.Quantity) : query.OrderBy(t => t.Quantity),
            _ => filter.SortDescending ? query.OrderByDescending(t => t.TransactionDate) : query.OrderBy(t => t.TransactionDate)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PartTransaction>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    #endregion

    #region Low Stock & Reorder

    public async Task<IEnumerable<Part>> GetLowStockPartsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Parts.Query()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Stocks)
            .Where(p => p.Status == PartStatus.Active)
            .Where(p => p.Stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved) <= p.ReorderPoint)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReorderStatus> GetReorderStatusAsync(int partId, CancellationToken cancellationToken = default)
    {
        var part = await _unitOfWork.Parts.Query()
            .Include(p => p.Stocks)
            .FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part == null)
            return ReorderStatus.Ok;

        var totalAvailable = part.Stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved);

        if (totalAvailable <= 0)
            return ReorderStatus.OutOfStock;
        if (totalAvailable <= part.MinStockLevel)
            return ReorderStatus.Critical;
        if (totalAvailable <= part.ReorderPoint)
            return ReorderStatus.Low;

        return ReorderStatus.Ok;
    }

    #endregion

    #region Asset Parts

    public async Task<AssetPart> UsePartOnAssetAsync(int assetId, int partId, int locationId, int quantity, decimal? unitCostOverride, int? workOrderId, string? notes, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be positive");

        var part = await _unitOfWork.Parts.GetByIdAsync(partId, cancellationToken);
        if (part == null)
            throw new InvalidOperationException("Part not found");

        var stock = await GetPartStockAsync(partId, locationId, cancellationToken);
        if (stock == null || stock.QuantityAvailable < quantity)
            throw new InvalidOperationException("Insufficient available stock");

        var unitCost = unitCostOverride ?? part.UnitCost;

        stock.QuantityOnHand -= quantity;
        stock.UpdatedBy = userId;
        stock.UpdatedAt = DateTime.UtcNow;

        var transaction = new PartTransaction
        {
            PartId = partId,
            LocationId = locationId,
            TransactionType = TransactionType.Issue,
            Quantity = -quantity,
            UnitCost = unitCost,
            ReferenceType = "Asset",
            ReferenceId = assetId,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        var assetPart = new AssetPart
        {
            AssetId = assetId,
            PartId = partId,
            QuantityUsed = quantity,
            UnitCostAtTime = unitCost,
            UsedDate = DateTime.UtcNow,
            UsedBy = userId,
            WorkOrderId = workOrderId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PartTransactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.AssetParts.AddAsync(assetPart, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return assetPart;
    }

    public async Task<PagedResult<AssetPart>> GetAssetPartsAsync(AssetPartFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.AssetParts.Query()
            .Include(ap => ap.Asset)
            .Include(ap => ap.Part)
            .Include(ap => ap.UsedByUser)
            .AsQueryable();

        if (filter.AssetId.HasValue)
        {
            query = query.Where(ap => ap.AssetId == filter.AssetId.Value);
        }

        if (filter.PartId.HasValue)
        {
            query = query.Where(ap => ap.PartId == filter.PartId.Value);
        }

        if (filter.WorkOrderId.HasValue)
        {
            query = query.Where(ap => ap.WorkOrderId == filter.WorkOrderId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(ap => ap.UsedDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(ap => ap.UsedDate <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "asset" => filter.SortDescending ? query.OrderByDescending(ap => ap.Asset.Name) : query.OrderBy(ap => ap.Asset.Name),
            "part" => filter.SortDescending ? query.OrderByDescending(ap => ap.Part.Name) : query.OrderBy(ap => ap.Part.Name),
            "quantity" => filter.SortDescending ? query.OrderByDescending(ap => ap.QuantityUsed) : query.OrderBy(ap => ap.QuantityUsed),
            _ => filter.SortDescending ? query.OrderByDescending(ap => ap.UsedDate) : query.OrderBy(ap => ap.UsedDate)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AssetPart>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    #endregion
}
