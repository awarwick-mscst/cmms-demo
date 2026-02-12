namespace CMMS.Core.Interfaces;

public interface ICalendarProvider
{
    string ProviderName { get; }
    Task<CalendarEventResult> CreateSharedEventAsync(CalendarEventRequest request, CancellationToken cancellationToken = default);
    Task<CalendarEventResult> CreateUserEventAsync(string userEmail, CalendarEventRequest request, CancellationToken cancellationToken = default);
    Task<CalendarEventResult> UpdateSharedEventAsync(string eventId, CalendarEventRequest request, CancellationToken cancellationToken = default);
    Task<CalendarEventResult> UpdateUserEventAsync(string userEmail, string eventId, CalendarEventRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSharedEventAsync(string eventId, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserEventAsync(string userEmail, string eventId, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

public class CalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public List<string> Attendees { get; set; } = new();
    public bool IsAllDay { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
}

public class CalendarEventResult
{
    public bool Success { get; set; }
    public string? EventId { get; set; }
    public string? ErrorMessage { get; set; }

    public static CalendarEventResult Succeeded(string eventId) => new()
    {
        Success = true,
        EventId = eventId
    };

    public static CalendarEventResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
