namespace CMMS.Core.Entities;

public class AssetPart : BaseEntityWithoutAudit
{
    public int AssetId { get; set; }
    public int PartId { get; set; }
    public int QuantityUsed { get; set; }
    public decimal UnitCostAtTime { get; set; }
    public DateTime UsedDate { get; set; } = DateTime.UtcNow;
    public int? UsedBy { get; set; }
    public int? WorkOrderId { get; set; }
    public string? Notes { get; set; }

    // Computed property
    public decimal TotalCost => QuantityUsed * UnitCostAtTime;

    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual Part Part { get; set; } = null!;
    public virtual User? UsedByUser { get; set; }
}
