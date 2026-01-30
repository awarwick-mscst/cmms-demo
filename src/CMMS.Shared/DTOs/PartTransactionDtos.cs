namespace CMMS.Shared.DTOs;

public class PartTransactionDto
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int? ToLocationId { get; set; }
    public string? ToLocationName { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
