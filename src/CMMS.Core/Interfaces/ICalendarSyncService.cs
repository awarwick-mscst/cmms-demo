using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface ICalendarSyncService
{
    // Work Order calendar sync
    Task SyncWorkOrderToCalendarAsync(int workOrderId, CancellationToken cancellationToken = default);
    Task RemoveWorkOrderFromCalendarAsync(int workOrderId, CancellationToken cancellationToken = default);
    Task UpdateWorkOrderCalendarEventAsync(int workOrderId, CancellationToken cancellationToken = default);

    // Preventive Maintenance calendar sync
    Task SyncPMScheduleToCalendarAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task RemovePMScheduleFromCalendarAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task UpdatePMScheduleCalendarEventAsync(int scheduleId, CancellationToken cancellationToken = default);

    // Calendar event management
    Task<IEnumerable<CalendarEvent>> GetCalendarEventsForReferenceAsync(string referenceType, int referenceId, CancellationToken cancellationToken = default);
    Task<CalendarEvent?> GetCalendarEventByExternalIdAsync(string externalEventId, CancellationToken cancellationToken = default);
}
