namespace CMMS.Core.Entities;

public class LabelTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public int Dpi { get; set; } = 203;
    public string ElementsJson { get; set; } = "[]";
    public bool IsDefault { get; set; }
}
