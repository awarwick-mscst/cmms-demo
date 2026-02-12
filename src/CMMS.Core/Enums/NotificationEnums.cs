namespace CMMS.Core.Enums;

/// <summary>
/// Type of notification to send
/// </summary>
public enum NotificationType
{
    WorkOrderAssigned = 0,
    WorkOrderApproachingDue = 1,
    WorkOrderOverdue = 2,
    WorkOrderCompleted = 3,
    PMScheduleComingDue = 4,
    PMScheduleOverdue = 5,
    LowStockAlert = 6
}

/// <summary>
/// Status of a notification in the queue
/// </summary>
public enum NotificationStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3
}

/// <summary>
/// Channel through which notification was delivered
/// </summary>
public enum NotificationChannel
{
    Email = 0,
    Calendar = 1,
    Teams = 2
}

/// <summary>
/// Type of calendar for calendar events
/// </summary>
public enum CalendarType
{
    Shared,
    User
}

/// <summary>
/// Type of integration provider
/// </summary>
public enum IntegrationProviderType
{
    MicrosoftGraph,
    Gmail,
    GoogleWorkspace
}
