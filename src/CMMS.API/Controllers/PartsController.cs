using CMMS.API.Attributes;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[RequiresFeature("inventory")]
public class PartsController : ControllerBase
{
    private readonly IPartService _partService;
    private readonly ICurrentUserService _currentUserService;

    public PartsController(IPartService partService, ICurrentUserService currentUserService)
    {
        _partService = partService;
        _currentUserService = currentUserService;
    }

    #region Part CRUD

    [HttpGet]
    public async Task<ActionResult<PagedResponse<PartDto>>> GetParts(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? supplierId,
        [FromQuery] string? status,
        [FromQuery] bool? lowStock,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new PartFilter
        {
            Search = search,
            CategoryId = categoryId,
            SupplierId = supplierId,
            Status = status,
            LowStock = lowStock,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _partService.GetPartsAsync(filter, cancellationToken);

        var response = new PagedResponse<PartDto>
        {
            Items = result.Items.Select(MapToDto),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PartDetailDto>>> GetPart(int id, CancellationToken cancellationToken = default)
    {
        var part = await _partService.GetPartByIdAsync(id, cancellationToken);

        if (part == null)
            return NotFound(ApiResponse<PartDetailDto>.Fail("Part not found"));

        var reorderStatus = await _partService.GetReorderStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<PartDetailDto>.Ok(MapToDetailDto(part, reorderStatus)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<PartDto>>> CreatePart(
        [FromBody] CreatePartRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PartDto>.Fail("User not authenticated"));

        if (!string.IsNullOrEmpty(request.PartNumber) && await _partService.PartNumberExistsAsync(request.PartNumber, null, cancellationToken))
            return BadRequest(ApiResponse<PartDto>.Fail("Part number already exists"));

        var part = new Part
        {
            PartNumber = request.PartNumber ?? string.Empty,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            UnitOfMeasure = Enum.TryParse<UnitOfMeasure>(request.UnitOfMeasure, out var uom) ? uom : UnitOfMeasure.Each,
            UnitCost = request.UnitCost,
            ReorderPoint = request.ReorderPoint,
            ReorderQuantity = request.ReorderQuantity,
            Status = Enum.TryParse<PartStatus>(request.Status, out var status) ? status : PartStatus.Active,
            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,
            LeadTimeDays = request.LeadTimeDays,
            Specifications = request.Specifications,
            Manufacturer = request.Manufacturer,
            ManufacturerPartNumber = request.ManufacturerPartNumber,
            Barcode = request.Barcode,
            ImageUrl = request.ImageUrl,
            Notes = request.Notes
        };

        var created = await _partService.CreatePartAsync(part, userId.Value, cancellationToken);
        var result = await _partService.GetPartByIdAsync(created.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetPart),
            new { id = created.Id },
            ApiResponse<PartDto>.Ok(MapToDto(result!), "Part created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<PartDto>>> UpdatePart(
        int id,
        [FromBody] UpdatePartRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PartDto>.Fail("User not authenticated"));

        var part = await _partService.GetPartByIdAsync(id, cancellationToken);
        if (part == null)
            return NotFound(ApiResponse<PartDto>.Fail("Part not found"));

        part.Name = request.Name;
        part.Description = request.Description;
        part.CategoryId = request.CategoryId;
        part.SupplierId = request.SupplierId;
        part.UnitOfMeasure = Enum.TryParse<UnitOfMeasure>(request.UnitOfMeasure, out var uom) ? uom : UnitOfMeasure.Each;
        part.UnitCost = request.UnitCost;
        part.ReorderPoint = request.ReorderPoint;
        part.ReorderQuantity = request.ReorderQuantity;
        part.Status = Enum.TryParse<PartStatus>(request.Status, out var status) ? status : PartStatus.Active;
        part.MinStockLevel = request.MinStockLevel;
        part.MaxStockLevel = request.MaxStockLevel;
        part.LeadTimeDays = request.LeadTimeDays;
        part.Specifications = request.Specifications;
        part.Manufacturer = request.Manufacturer;
        part.ManufacturerPartNumber = request.ManufacturerPartNumber;
        part.Barcode = request.Barcode;
        part.ImageUrl = request.ImageUrl;
        part.Notes = request.Notes;

        await _partService.UpdatePartAsync(part, userId.Value, cancellationToken);
        var result = await _partService.GetPartByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<PartDto>.Ok(MapToDto(result!), "Part updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse>> DeletePart(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Fail("User not authenticated"));

        var success = await _partService.DeletePartAsync(id, userId.Value, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Part not found"));

        return Ok(ApiResponse.Ok("Part deleted successfully"));
    }

    #endregion

    #region Stock Management

    [HttpGet("{id}/stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PartStockDto>>>> GetPartStock(int id, CancellationToken cancellationToken = default)
    {
        var stocks = await _partService.GetPartStocksAsync(id, cancellationToken);
        var dtos = stocks.Select(MapStockToDto);
        return Ok(ApiResponse<IEnumerable<PartStockDto>>.Ok(dtos));
    }

    [HttpPost("{id}/stock/adjust")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<PartStockDto>>> AdjustStock(
        int id,
        [FromBody] StockAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PartStockDto>.Fail("User not authenticated"));

        if (!Enum.TryParse<TransactionType>(request.TransactionType, out var transactionType))
            return BadRequest(ApiResponse<PartStockDto>.Fail("Invalid transaction type"));

        if (transactionType != TransactionType.Receive && transactionType != TransactionType.Issue && transactionType != TransactionType.Adjust)
            return BadRequest(ApiResponse<PartStockDto>.Fail("Transaction type must be Receive, Issue, or Adjust"));

        try
        {
            var stock = await _partService.AdjustStockAsync(
                id,
                request.LocationId,
                transactionType,
                request.Quantity,
                request.UnitCost,
                request.ReferenceType,
                request.ReferenceId,
                request.Notes,
                userId.Value,
                cancellationToken);

            return Ok(ApiResponse<PartStockDto>.Ok(MapStockToDto(stock), "Stock adjusted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PartStockDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/stock/transfer")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<object>>> TransferStock(
        int id,
        [FromBody] StockTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Fail("User not authenticated"));

        try
        {
            var (from, to) = await _partService.TransferStockAsync(
                id,
                request.FromLocationId,
                request.ToLocationId,
                request.Quantity,
                request.Notes,
                userId.Value,
                cancellationToken);

            return Ok(ApiResponse<object>.Ok(new
            {
                FromStock = MapStockToDto(from),
                ToStock = MapStockToDto(to)
            }, "Stock transferred successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/stock/reserve")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<PartStockDto>>> ReserveStock(
        int id,
        [FromBody] StockReserveRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PartStockDto>.Fail("User not authenticated"));

        try
        {
            var stock = await _partService.ReserveStockAsync(
                id,
                request.LocationId,
                request.Quantity,
                request.ReferenceType,
                request.ReferenceId,
                request.Notes,
                userId.Value,
                cancellationToken);

            return Ok(ApiResponse<PartStockDto>.Ok(MapStockToDto(stock), "Stock reserved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PartStockDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/stock/unreserve")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<PartStockDto>>> UnreserveStock(
        int id,
        [FromBody] StockReserveRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PartStockDto>.Fail("User not authenticated"));

        try
        {
            var stock = await _partService.UnreserveStockAsync(
                id,
                request.LocationId,
                request.Quantity,
                request.ReferenceType,
                request.ReferenceId,
                request.Notes,
                userId.Value,
                cancellationToken);

            return Ok(ApiResponse<PartStockDto>.Ok(MapStockToDto(stock), "Stock unreserved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PartStockDto>.Fail(ex.Message));
        }
    }

    #endregion

    #region Transaction History

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<PagedResponse<PartTransactionDto>>> GetPartTransactions(
        int id,
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new PartTransactionFilter
        {
            PartId = id,
            TransactionType = transactionType,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            SortDescending = true
        };

        var result = await _partService.GetPartTransactionsAsync(filter, cancellationToken);

        var response = new PagedResponse<PartTransactionDto>
        {
            Items = result.Items.Select(MapTransactionToDto),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        };

        return Ok(response);
    }

    #endregion

    #region Low Stock

    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PartDto>>>> GetLowStockParts(CancellationToken cancellationToken = default)
    {
        var parts = await _partService.GetLowStockPartsAsync(cancellationToken);
        var dtos = parts.Select(MapToDto);
        return Ok(ApiResponse<IEnumerable<PartDto>>.Ok(dtos));
    }

    #endregion

    #region Asset Parts

    [HttpPost("{id}/use-on-asset")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<AssetPartDto>>> UsePartOnAsset(
        int id,
        [FromBody] CreateAssetPartRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<AssetPartDto>.Fail("User not authenticated"));

        try
        {
            var assetPart = await _partService.UsePartOnAssetAsync(
                request.AssetId,
                id,
                request.LocationId,
                request.QuantityUsed,
                request.UnitCostOverride,
                request.WorkOrderId,
                request.Notes,
                userId.Value,
                cancellationToken);

            return Ok(ApiResponse<AssetPartDto>.Ok(MapAssetPartToDto(assetPart), "Part used on asset successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AssetPartDto>.Fail(ex.Message));
        }
    }

    #endregion

    #region Mapping

    private static PartDto MapToDto(Part part)
    {
        var totalOnHand = part.Stocks?.Sum(s => s.QuantityOnHand) ?? 0;
        var totalReserved = part.Stocks?.Sum(s => s.QuantityReserved) ?? 0;
        var totalAvailable = totalOnHand - totalReserved;

        ReorderStatus reorderStatus;
        if (totalAvailable <= 0)
            reorderStatus = ReorderStatus.OutOfStock;
        else if (totalAvailable <= part.MinStockLevel)
            reorderStatus = ReorderStatus.Critical;
        else if (totalAvailable <= part.ReorderPoint)
            reorderStatus = ReorderStatus.Low;
        else
            reorderStatus = ReorderStatus.Ok;

        return new PartDto
        {
            Id = part.Id,
            PartNumber = part.PartNumber,
            Name = part.Name,
            Description = part.Description,
            CategoryId = part.CategoryId,
            CategoryName = part.Category?.Name,
            SupplierId = part.SupplierId,
            SupplierName = part.Supplier?.Name,
            UnitOfMeasure = part.UnitOfMeasure.ToString(),
            UnitCost = part.UnitCost,
            ReorderPoint = part.ReorderPoint,
            ReorderQuantity = part.ReorderQuantity,
            Status = part.Status.ToString(),
            MinStockLevel = part.MinStockLevel,
            MaxStockLevel = part.MaxStockLevel,
            LeadTimeDays = part.LeadTimeDays,
            Specifications = part.Specifications,
            Manufacturer = part.Manufacturer,
            ManufacturerPartNumber = part.ManufacturerPartNumber,
            Barcode = part.Barcode,
            ImageUrl = part.ImageUrl,
            Notes = part.Notes,
            TotalQuantityOnHand = totalOnHand,
            TotalQuantityReserved = totalReserved,
            TotalQuantityAvailable = totalAvailable,
            ReorderStatus = reorderStatus.ToString(),
            CreatedAt = part.CreatedAt,
            UpdatedAt = part.UpdatedAt
        };
    }

    private static PartDetailDto MapToDetailDto(Part part, ReorderStatus reorderStatus)
    {
        var dto = MapToDto(part);
        return new PartDetailDto
        {
            Id = dto.Id,
            PartNumber = dto.PartNumber,
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            SupplierId = dto.SupplierId,
            SupplierName = dto.SupplierName,
            UnitOfMeasure = dto.UnitOfMeasure,
            UnitCost = dto.UnitCost,
            ReorderPoint = dto.ReorderPoint,
            ReorderQuantity = dto.ReorderQuantity,
            Status = dto.Status,
            MinStockLevel = dto.MinStockLevel,
            MaxStockLevel = dto.MaxStockLevel,
            LeadTimeDays = dto.LeadTimeDays,
            Specifications = dto.Specifications,
            Manufacturer = dto.Manufacturer,
            ManufacturerPartNumber = dto.ManufacturerPartNumber,
            Barcode = dto.Barcode,
            ImageUrl = dto.ImageUrl,
            Notes = dto.Notes,
            TotalQuantityOnHand = dto.TotalQuantityOnHand,
            TotalQuantityReserved = dto.TotalQuantityReserved,
            TotalQuantityAvailable = dto.TotalQuantityAvailable,
            ReorderStatus = reorderStatus.ToString(),
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Stocks = part.Stocks?.Select(MapStockToDto).ToList() ?? new List<PartStockDto>()
        };
    }

    private static PartStockDto MapStockToDto(PartStock stock)
    {
        return new PartStockDto
        {
            Id = stock.Id,
            PartId = stock.PartId,
            LocationId = stock.LocationId,
            LocationName = stock.Location?.Name ?? string.Empty,
            LocationFullPath = stock.Location?.FullPath,
            QuantityOnHand = stock.QuantityOnHand,
            QuantityReserved = stock.QuantityReserved,
            QuantityAvailable = stock.QuantityAvailable,
            LastCountDate = stock.LastCountDate,
            LastCountByName = stock.LastCountByUser != null ? $"{stock.LastCountByUser.FirstName} {stock.LastCountByUser.LastName}" : null,
            BinNumber = stock.BinNumber,
            ShelfLocation = stock.ShelfLocation
        };
    }

    private static PartTransactionDto MapTransactionToDto(PartTransaction transaction)
    {
        return new PartTransactionDto
        {
            Id = transaction.Id,
            PartId = transaction.PartId,
            PartNumber = transaction.Part?.PartNumber ?? string.Empty,
            PartName = transaction.Part?.Name ?? string.Empty,
            LocationId = transaction.LocationId,
            LocationName = transaction.Location?.Name,
            ToLocationId = transaction.ToLocationId,
            ToLocationName = transaction.ToLocation?.Name,
            TransactionType = transaction.TransactionType.ToString(),
            Quantity = transaction.Quantity,
            UnitCost = transaction.UnitCost,
            TotalCost = Math.Abs(transaction.Quantity) * transaction.UnitCost,
            ReferenceType = transaction.ReferenceType,
            ReferenceId = transaction.ReferenceId,
            Notes = transaction.Notes,
            TransactionDate = transaction.TransactionDate,
            CreatedBy = transaction.CreatedBy,
            CreatedByName = transaction.CreatedByUser != null ? $"{transaction.CreatedByUser.FirstName} {transaction.CreatedByUser.LastName}" : null,
            CreatedAt = transaction.CreatedAt
        };
    }

    private static AssetPartDto MapAssetPartToDto(AssetPart assetPart)
    {
        return new AssetPartDto
        {
            Id = assetPart.Id,
            AssetId = assetPart.AssetId,
            AssetTag = assetPart.Asset?.AssetTag ?? string.Empty,
            AssetName = assetPart.Asset?.Name ?? string.Empty,
            PartId = assetPart.PartId,
            PartNumber = assetPart.Part?.PartNumber ?? string.Empty,
            PartName = assetPart.Part?.Name ?? string.Empty,
            QuantityUsed = assetPart.QuantityUsed,
            UnitCostAtTime = assetPart.UnitCostAtTime,
            TotalCost = assetPart.TotalCost,
            UsedDate = assetPart.UsedDate,
            UsedBy = assetPart.UsedBy,
            UsedByName = assetPart.UsedByUser != null ? $"{assetPart.UsedByUser.FirstName} {assetPart.UsedByUser.LastName}" : null,
            WorkOrderId = assetPart.WorkOrderId,
            Notes = assetPart.Notes,
            CreatedAt = assetPart.CreatedAt
        };
    }

    #endregion
}
