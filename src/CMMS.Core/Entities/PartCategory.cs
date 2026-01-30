namespace CMMS.Core.Entities;

public class PartCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual PartCategory? Parent { get; set; }
    public virtual ICollection<PartCategory> Children { get; set; } = new List<PartCategory>();
    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();
}
