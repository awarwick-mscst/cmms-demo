namespace CMMS.Core.Entities;

public class StorageLocation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public string? FullPath { get; set; }
    public string? Building { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual StorageLocation? Parent { get; set; }
    public virtual ICollection<StorageLocation> Children { get; set; } = new List<StorageLocation>();
    public virtual ICollection<PartStock> PartStocks { get; set; } = new List<PartStock>();
}
