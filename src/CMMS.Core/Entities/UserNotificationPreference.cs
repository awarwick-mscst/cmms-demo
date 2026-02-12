using CMMS.Core.Enums;

namespace CMMS.Core.Entities;

public class UserNotificationPreference : BaseEntity
{
    public int UserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool CalendarEnabled { get; set; } = true;

    // Navigation properties
    public virtual User? User { get; set; }
}
