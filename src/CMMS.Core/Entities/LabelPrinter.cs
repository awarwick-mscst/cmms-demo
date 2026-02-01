namespace CMMS.Core.Entities;

public class LabelPrinter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
    public string? PrinterModel { get; set; }
    public int Dpi { get; set; } = 203;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
