namespace CMMS.Shared.DTOs;

/// <summary>
/// Work order labor entry details
/// </summary>
public class WorkOrderLaborDto
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string LaborType { get; set; } = string.Empty;
    public decimal? HourlyRate { get; set; }
    public decimal? TotalCost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create a new labor entry
/// </summary>
public class CreateWorkOrderLaborRequest
{
    public int UserId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string LaborType { get; set; } = "Regular";
    public decimal? HourlyRate { get; set; }
    public string? Notes { get; set; }
}
