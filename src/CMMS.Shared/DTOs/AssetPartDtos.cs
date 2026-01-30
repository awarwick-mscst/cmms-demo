namespace CMMS.Shared.DTOs;

public class AssetPartDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int QuantityUsed { get; set; }
    public decimal UnitCostAtTime { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime UsedDate { get; set; }
    public int? UsedBy { get; set; }
    public string? UsedByName { get; set; }
    public int? WorkOrderId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAssetPartRequest
{
    public int AssetId { get; set; }
    public int PartId { get; set; }
    public int LocationId { get; set; }
    public int QuantityUsed { get; set; }
    public decimal? UnitCostOverride { get; set; }
    public int? WorkOrderId { get; set; }
    public string? Notes { get; set; }
}
