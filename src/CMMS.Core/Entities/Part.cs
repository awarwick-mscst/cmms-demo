using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

public class Part : BaseEntity
{
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Each;
    public decimal UnitCost { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public PartStatus Status { get; set; } = PartStatus.Active;
    public int MinStockLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public int LeadTimeDays { get; set; }
    public string? Specifications { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual PartCategory? Category { get; set; }
    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<PartStock> Stocks { get; set; } = new List<PartStock>();
    public virtual ICollection<PartTransaction> Transactions { get; set; } = new List<PartTransaction>();
    public virtual ICollection<AssetPart> AssetParts { get; set; } = new List<AssetPart>();
}
