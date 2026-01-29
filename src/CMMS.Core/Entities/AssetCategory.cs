namespace CMMS.Core.Entities;

public class AssetCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual AssetCategory? Parent { get; set; }
    public virtual ICollection<AssetCategory> Children { get; set; } = new List<AssetCategory>();
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
