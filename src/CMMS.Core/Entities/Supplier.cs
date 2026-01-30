namespace CMMS.Core.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();
}
