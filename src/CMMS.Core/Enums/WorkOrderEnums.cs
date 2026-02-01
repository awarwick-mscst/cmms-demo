namespace CMMS.Core.Enums;

/// <summary>
/// Type of work order
/// </summary>
public enum WorkOrderType
{
    Repair,
    ScheduledJob,
    SafetyInspection,
    PreventiveMaintenance
}

/// <summary>
/// Work order status representing the lifecycle stages
/// </summary>
public enum WorkOrderStatus
{
    Draft,
    Open,
    InProgress,
    OnHold,
    Completed,
    Cancelled
}

/// <summary>
/// Priority level for work orders
/// </summary>
public enum WorkOrderPriority
{
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

/// <summary>
/// Type of labor for labor entries
/// </summary>
public enum LaborType
{
    Regular,
    Overtime,
    Emergency
}

/// <summary>
/// Frequency type for preventive maintenance schedules
/// </summary>
public enum FrequencyType
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually,
    Custom
}
