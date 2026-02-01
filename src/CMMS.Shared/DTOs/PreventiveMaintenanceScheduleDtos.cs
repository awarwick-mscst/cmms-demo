namespace CMMS.Shared.DTOs;

/// <summary>
/// Full PM schedule details
/// </summary>
public class PreventiveMaintenanceScheduleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? AssetTag { get; set; }
    public string FrequencyType { get; set; } = string.Empty;
    public int FrequencyValue { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public DateTime? NextDueDate { get; set; }
    public DateTime? LastCompletedDate { get; set; }
    public int LeadTimeDays { get; set; }
    public string WorkOrderTitle { get; set; } = string.Empty;
    public string? WorkOrderDescription { get; set; }
    public string Priority { get; set; } = string.Empty;
    public decimal? EstimatedHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Summary view for PM schedule lists
/// </summary>
public class PreventiveMaintenanceScheduleSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public string FrequencyType { get; set; } = string.Empty;
    public int FrequencyValue { get; set; }
    public DateTime? NextDueDate { get; set; }
    public DateTime? LastCompletedDate { get; set; }
    public string Priority { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Request to create a new PM schedule
/// </summary>
public class CreatePreventiveMaintenanceScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AssetId { get; set; }
    public string FrequencyType { get; set; } = "Monthly";
    public int FrequencyValue { get; set; } = 1;
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int LeadTimeDays { get; set; }
    public string WorkOrderTitle { get; set; } = string.Empty;
    public string? WorkOrderDescription { get; set; }
    public string Priority { get; set; } = "Medium";
    public decimal? EstimatedHours { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request to update an existing PM schedule
/// </summary>
public class UpdatePreventiveMaintenanceScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AssetId { get; set; }
    public string FrequencyType { get; set; } = "Monthly";
    public int FrequencyValue { get; set; } = 1;
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int LeadTimeDays { get; set; }
    public string WorkOrderTitle { get; set; } = string.Empty;
    public string? WorkOrderDescription { get; set; }
    public string Priority { get; set; } = "Medium";
    public decimal? EstimatedHours { get; set; }
    public bool IsActive { get; set; } = true;
}
