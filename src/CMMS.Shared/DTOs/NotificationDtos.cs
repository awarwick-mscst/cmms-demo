namespace CMMS.Shared.DTOs;

// Notification Queue DTOs
public class NotificationQueueDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime ScheduledFor { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Notification Log DTOs
public class NotificationLogDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
}

// User Notification Preferences DTOs
public class UserNotificationPreferenceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string NotificationTypeDisplay { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; }
    public bool CalendarEnabled { get; set; }
}

public class UpdateNotificationPreferenceRequest
{
    public string NotificationType { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; }
    public bool CalendarEnabled { get; set; }
}

public class BulkUpdateNotificationPreferencesRequest
{
    public List<UpdateNotificationPreferenceRequest> Preferences { get; set; } = new();
}

// Integration Settings DTOs
public class IntegrationSettingsDto
{
    public string ProviderType { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public bool IsValid { get; set; }
    public DateTime? LastValidated { get; set; }
}

public class MicrosoftGraphSettingsDto
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string SharedMailbox { get; set; } = string.Empty;
    public string SharedCalendarId { get; set; } = string.Empty;
    public string TeamsWebhookUrl { get; set; } = string.Empty;
}

public class UpdateMicrosoftGraphSettingsRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string SharedMailbox { get; set; } = string.Empty;
    public string? SharedCalendarId { get; set; }
    public string? TeamsWebhookUrl { get; set; }
}

public class TestEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
}

public class TestCalendarEventRequest
{
    public string Title { get; set; } = "CMMS Test Event";
    public DateTime? StartTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
}

// Calendar Event DTOs
public class CalendarEventDto
{
    public int Id { get; set; }
    public string ExternalEventId { get; set; } = string.Empty;
    public string CalendarType { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Notification System Stats
public class NotificationStatsDto
{
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int SentToday { get; set; }
    public int FailedToday { get; set; }
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
}

// Notification Queue Filter
public class NotificationQueueFilter
{
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// Notification Log Filter
public class NotificationLogFilter
{
    public bool? Success { get; set; }
    public string? Type { get; set; }
    public string? Channel { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// Notification Type Info
public class NotificationTypeInfo
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
