namespace CMMS.Core.Entities;

public class AssetLocation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public string? FullPath { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual AssetLocation? Parent { get; set; }
    public virtual ICollection<AssetLocation> Children { get; set; } = new List<AssetLocation>();
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
