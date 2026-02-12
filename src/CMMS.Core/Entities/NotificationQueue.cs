using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

public class NotificationQueue : BaseEntity
{
    public NotificationType Type { get; set; }
    public int? RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }

    // Navigation properties
    public virtual User? RecipientUser { get; set; }
}
