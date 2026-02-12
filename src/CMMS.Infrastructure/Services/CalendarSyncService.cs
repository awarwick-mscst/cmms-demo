using CMMS.Core.Configuration;
using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMMS.Infrastructure.Services;

public class CalendarSyncService : ICalendarSyncService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICalendarProvider _calendarProvider;
    private readonly EmailCalendarSettings _settings;
    private readonly ILogger<CalendarSyncService> _logger;

    public CalendarSyncService(
        IUnitOfWork unitOfWork,
        ICalendarProvider calendarProvider,
        IOptions<EmailCalendarSettings> settings,
        ILogger<CalendarSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _calendarProvider = calendarProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SyncWorkOrderToCalendarAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return;

        var workOrder = await _unitOfWork.WorkOrders.Query()
            .Include(w => w.AssignedTo)
            .Include(w => w.Asset)
            .Include(w => w.Location)
            .FirstOrDefaultAsync(w => w.Id == workOrderId, cancellationToken);

        if (workOrder == null || !workOrder.ScheduledStartDate.HasValue)
            return;

        var startTime = workOrder.ScheduledStartDate.Value;
        var endTime = workOrder.ScheduledEndDate ?? startTime.AddMinutes(_settings.Calendar.DefaultEventDurationMinutes);

        var request = new CalendarEventRequest
        {
            Title = $"[WO] {workOrder.WorkOrderNumber} - {workOrder.Title}",
            Description = $"Work Order: {workOrder.WorkOrderNumber}\nAsset: {workOrder.Asset?.Name ?? "N/A"}\nPriority: {workOrder.Priority}\n\n{workOrder.Description}",
            StartTime = startTime,
            EndTime = endTime,
            Location = workOrder.Location?.Name,
            ReferenceType = "WorkOrder",
            ReferenceId = workOrderId
        };

        // Add assignee as attendee
        if (workOrder.AssignedTo != null && !string.IsNullOrEmpty(workOrder.AssignedTo.Email))
        {
            request.Attendees.Add(workOrder.AssignedTo.Email);
        }

        // Sync to shared calendar
        if (_settings.Calendar.SyncToSharedCalendar)
        {
            await CreateOrUpdateCalendarEventAsync(request, "Shared", null, cancellationToken);
        }

        // Sync to user calendar
        if (_settings.Calendar.SyncToUserCalendars && workOrder.AssignedTo != null && !string.IsNullOrEmpty(workOrder.AssignedTo.Email))
        {
            await CreateOrUpdateCalendarEventAsync(request, "User", workOrder.AssignedTo, cancellationToken);
        }
    }

    public async Task RemoveWorkOrderFromCalendarAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var events = await GetCalendarEventsForReferenceAsync("WorkOrder", workOrderId, cancellationToken);

        foreach (var calendarEvent in events)
        {
            try
            {
                bool deleted;
                if (calendarEvent.CalendarType == "Shared")
                {
                    deleted = await _calendarProvider.DeleteSharedEventAsync(calendarEvent.ExternalEventId, cancellationToken);
                }
                else
                {
                    var user = calendarEvent.UserId.HasValue
                        ? await _unitOfWork.Users.GetByIdAsync(calendarEvent.UserId.Value, cancellationToken)
                        : null;

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        deleted = await _calendarProvider.DeleteUserEventAsync(user.Email, calendarEvent.ExternalEventId, cancellationToken);
                    }
                    else
                    {
                        deleted = true; // Can't delete, mark as done
                    }
                }

                if (deleted)
                {
                    calendarEvent.IsDeleted = true;
                    calendarEvent.DeletedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete calendar event {EventId}", calendarEvent.ExternalEventId);
            }
        }
    }

    public async Task UpdateWorkOrderCalendarEventAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        // For simplicity, remove old events and create new ones
        await RemoveWorkOrderFromCalendarAsync(workOrderId, cancellationToken);
        await SyncWorkOrderToCalendarAsync(workOrderId, cancellationToken);
    }

    public async Task SyncPMScheduleToCalendarAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return;

        var schedule = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(s => s.Asset)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

        if (schedule == null || !schedule.NextDueDate.HasValue)
            return;

        var startTime = schedule.NextDueDate.Value;
        var endTime = startTime.AddMinutes(_settings.Calendar.DefaultEventDurationMinutes);

        var request = new CalendarEventRequest
        {
            Title = $"[PM] {schedule.Name}",
            Description = $"Preventive Maintenance: {schedule.Name}\nAsset: {schedule.Asset?.Name ?? "N/A"}\nFrequency: {schedule.FrequencyType}\n\n{schedule.Description}",
            StartTime = startTime,
            EndTime = endTime,
            ReferenceType = "PM",
            ReferenceId = scheduleId
        };

        // Sync to shared calendar only (no default assignee on PM schedules currently)
        if (_settings.Calendar.SyncToSharedCalendar)
        {
            await CreateOrUpdateCalendarEventAsync(request, "Shared", null, cancellationToken);
        }
    }

    public async Task RemovePMScheduleFromCalendarAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var events = await GetCalendarEventsForReferenceAsync("PM", scheduleId, cancellationToken);

        foreach (var calendarEvent in events)
        {
            try
            {
                bool deleted;
                if (calendarEvent.CalendarType == "Shared")
                {
                    deleted = await _calendarProvider.DeleteSharedEventAsync(calendarEvent.ExternalEventId, cancellationToken);
                }
                else
                {
                    var user = calendarEvent.UserId.HasValue
                        ? await _unitOfWork.Users.GetByIdAsync(calendarEvent.UserId.Value, cancellationToken)
                        : null;

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        deleted = await _calendarProvider.DeleteUserEventAsync(user.Email, calendarEvent.ExternalEventId, cancellationToken);
                    }
                    else
                    {
                        deleted = true;
                    }
                }

                if (deleted)
                {
                    calendarEvent.IsDeleted = true;
                    calendarEvent.DeletedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete calendar event {EventId}", calendarEvent.ExternalEventId);
            }
        }
    }

    public async Task UpdatePMScheduleCalendarEventAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        await RemovePMScheduleFromCalendarAsync(scheduleId, cancellationToken);
        await SyncPMScheduleToCalendarAsync(scheduleId, cancellationToken);
    }

    public async Task<IEnumerable<CalendarEvent>> GetCalendarEventsForReferenceAsync(string referenceType, int referenceId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.CalendarEvents.Query()
            .Where(e => e.ReferenceType == referenceType && e.ReferenceId == referenceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<CalendarEvent?> GetCalendarEventByExternalIdAsync(string externalEventId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.CalendarEvents.Query()
            .FirstOrDefaultAsync(e => e.ExternalEventId == externalEventId, cancellationToken);
    }

    private async Task CreateOrUpdateCalendarEventAsync(CalendarEventRequest request, string calendarType, User? user, CancellationToken cancellationToken)
    {
        // Check for existing event
        int? userId = user?.Id;
        var existingEvent = await _unitOfWork.CalendarEvents.Query()
            .FirstOrDefaultAsync(e =>
                e.ReferenceType == request.ReferenceType
                && e.ReferenceId == request.ReferenceId
                && e.CalendarType == calendarType
                && e.UserId == userId,
                cancellationToken);

        CalendarEventResult result;

        if (existingEvent != null)
        {
            // Update existing event
            if (calendarType == "Shared")
            {
                result = await _calendarProvider.UpdateSharedEventAsync(existingEvent.ExternalEventId, request, cancellationToken);
            }
            else
            {
                result = await _calendarProvider.UpdateUserEventAsync(user!.Email, existingEvent.ExternalEventId, request, cancellationToken);
            }

            if (result.Success)
            {
                existingEvent.Title = request.Title;
                existingEvent.StartTime = request.StartTime;
                existingEvent.EndTime = request.EndTime;
                existingEvent.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            // Create new event
            if (calendarType == "Shared")
            {
                result = await _calendarProvider.CreateSharedEventAsync(request, cancellationToken);
            }
            else
            {
                result = await _calendarProvider.CreateUserEventAsync(user!.Email, request, cancellationToken);
            }

            if (result.Success && !string.IsNullOrEmpty(result.EventId))
            {
                var calendarEvent = new CalendarEvent
                {
                    ExternalEventId = result.EventId,
                    CalendarType = calendarType,
                    UserId = user?.Id,
                    ReferenceType = request.ReferenceType ?? string.Empty,
                    ReferenceId = request.ReferenceId ?? 0,
                    Title = request.Title,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    ProviderType = _calendarProvider.ProviderName,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CalendarEvents.AddAsync(calendarEvent, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        if (!result.Success)
        {
            _logger.LogWarning("Failed to sync calendar event for {ReferenceType} {ReferenceId}: {Error}",
                request.ReferenceType, request.ReferenceId, result.ErrorMessage);
        }
    }
}
