namespace CMMS.Shared.DTOs;

public class PartDto
{
    public int Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int MinStockLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public int LeadTimeDays { get; set; }
    public string? Specifications { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
    public int TotalQuantityOnHand { get; set; }
    public int TotalQuantityReserved { get; set; }
    public int TotalQuantityAvailable { get; set; }
    public string ReorderStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PartDetailDto : PartDto
{
    public List<PartStockDto> Stocks { get; set; } = new();
    public List<PartTransactionDto> RecentTransactions { get; set; } = new();
}

public class PartStockDto
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? LocationFullPath { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int QuantityAvailable { get; set; }
    public DateTime? LastCountDate { get; set; }
    public string? LastCountByName { get; set; }
    public string? BinNumber { get; set; }
    public string? ShelfLocation { get; set; }
}

public class CreatePartRequest
{
    public string? PartNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public string UnitOfMeasure { get; set; } = "Each";
    public decimal UnitCost { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string Status { get; set; } = "Active";
    public int MinStockLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public int LeadTimeDays { get; set; }
    public string? Specifications { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePartRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public string UnitOfMeasure { get; set; } = "Each";
    public decimal UnitCost { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string Status { get; set; } = "Active";
    public int MinStockLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public int LeadTimeDays { get; set; }
    public string? Specifications { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
}

public class StockAdjustmentRequest
{
    public int LocationId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
}

public class StockTransferRequest
{
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class StockReserveRequest
{
    public int LocationId { get; set; }
    public int Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
}
