using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

public class PartTransaction : BaseEntityWithoutAudit
{
    public int PartId { get; set; }
    public int? LocationId { get; set; }
    public int? ToLocationId { get; set; }
    public TransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    // Navigation properties
    public virtual Part Part { get; set; } = null!;
    public virtual StorageLocation? Location { get; set; }
    public virtual StorageLocation? ToLocation { get; set; }
    public virtual User? CreatedByUser { get; set; }
}
