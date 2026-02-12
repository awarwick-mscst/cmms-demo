using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

public class NotificationLog : BaseEntityWithoutAudit
{
    public NotificationType Type { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public bool Success { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int? QueueId { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }

    // Navigation properties
    public virtual NotificationQueue? Queue { get; set; }
}
