namespace CMMS.Core.Configuration;

/// <summary>
/// Configuration settings for email and calendar integration.
/// </summary>
public class EmailCalendarSettings
{
    public const string SectionName = "EmailCalendar";

    /// <summary>
    /// Enable or disable the email/calendar system. Default is false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The active provider to use for sending emails and calendar events.
    /// Supports: "MicrosoftGraph", "Gmail" (future)
    /// </summary>
    public string ActiveProvider { get; set; } = "MicrosoftGraph";

    /// <summary>
    /// Microsoft Graph API settings.
    /// </summary>
    public MicrosoftGraphSettings MicrosoftGraph { get; set; } = new();

    /// <summary>
    /// Notification processing settings.
    /// </summary>
    public NotificationProcessingSettings Notifications { get; set; } = new();

    /// <summary>
    /// Calendar sync settings.
    /// </summary>
    public CalendarSyncSettings Calendar { get; set; } = new();
}

/// <summary>
/// Microsoft Graph API configuration.
/// </summary>
public class MicrosoftGraphSettings
{
    /// <summary>
    /// Azure AD Tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Application (Client) ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Client Secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Shared mailbox to send emails from (e.g., cmms@yourdomain.com).
    /// </summary>
    public string SharedMailbox { get; set; } = string.Empty;

    /// <summary>
    /// ID of the shared calendar for maintenance events.
    /// </summary>
    public string SharedCalendarId { get; set; } = string.Empty;
}

/// <summary>
/// Settings for notification processing.
/// </summary>
public class NotificationProcessingSettings
{
    /// <summary>
    /// Days before work order due date to send reminder.
    /// </summary>
    public int WorkOrderDueReminderDays { get; set; } = 3;

    /// <summary>
    /// Days before PM schedule due date to send reminder.
    /// </summary>
    public int PMDueReminderDays { get; set; } = 7;

    /// <summary>
    /// Maximum number of retry attempts for failed notifications.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay in minutes between retry attempts.
    /// </summary>
    public int RetryDelayMinutes { get; set; } = 5;

    /// <summary>
    /// Interval in seconds for the background processing service.
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Number of notifications to process per batch.
    /// </summary>
    public int BatchSize { get; set; } = 20;
}

/// <summary>
/// Settings for calendar synchronization.
/// </summary>
public class CalendarSyncSettings
{
    /// <summary>
    /// Sync events to the shared calendar.
    /// </summary>
    public bool SyncToSharedCalendar { get; set; } = true;

    /// <summary>
    /// Sync events to individual user calendars.
    /// </summary>
    public bool SyncToUserCalendars { get; set; } = true;

    /// <summary>
    /// Default duration in minutes for calendar events.
    /// </summary>
    public int DefaultEventDurationMinutes { get; set; } = 60;
}
