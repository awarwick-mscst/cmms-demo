namespace CMMS.Core.Entities;

public class PartStock : BaseEntity
{
    public int PartId { get; set; }
    public int LocationId { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public DateTime? LastCountDate { get; set; }
    public int? LastCountBy { get; set; }
    public string? BinNumber { get; set; }
    public string? ShelfLocation { get; set; }

    // Computed property
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    // Navigation properties
    public virtual Part Part { get; set; } = null!;
    public virtual StorageLocation Location { get; set; } = null!;
    public virtual User? LastCountByUser { get; set; }
}
