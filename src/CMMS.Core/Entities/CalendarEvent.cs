namespace CMMS.Core.Entities;

public class CalendarEvent : BaseEntity
{
    public string ExternalEventId { get; set; } = string.Empty;
    public string CalendarType { get; set; } = "Shared";
    public int? UserId { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ProviderType { get; set; } = "MicrosoftGraph";

    // Navigation properties
    public virtual User? User { get; set; }
}
