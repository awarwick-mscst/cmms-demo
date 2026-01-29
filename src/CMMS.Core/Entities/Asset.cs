using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

public class Asset : BaseEntity
{
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public AssetCriticality Criticality { get; set; } = AssetCriticality.Medium;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public int? ExpectedLifeYears { get; set; }
    public DateTime? InstallationDate { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? ParentAssetId { get; set; }
    public int? AssignedTo { get; set; }
    public string? Notes { get; set; }
    public string? CustomFields { get; set; }

    // Navigation properties
    public virtual AssetCategory Category { get; set; } = null!;
    public virtual AssetLocation? Location { get; set; }
    public virtual Asset? ParentAsset { get; set; }
    public virtual User? AssignedUser { get; set; }
    public virtual ICollection<Asset> ChildAssets { get; set; } = new List<Asset>();
    public virtual ICollection<AssetDocument> Documents { get; set; } = new List<AssetDocument>();
}
