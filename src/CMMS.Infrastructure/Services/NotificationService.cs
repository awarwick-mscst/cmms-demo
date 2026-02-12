using CMMS.Core.Configuration;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMMS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailProvider _emailProvider;
    private readonly EmailCalendarSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IEmailProvider emailProvider,
        IOptions<EmailCalendarSettings> settings,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailProvider = emailProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task QueueWorkOrderAssignedAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.WorkOrders.Query()
            .Include(w => w.AssignedTo)
            .Include(w => w.Asset)
            .FirstOrDefaultAsync(w => w.Id == workOrderId, cancellationToken);

        if (workOrder?.AssignedTo == null || string.IsNullOrEmpty(workOrder.AssignedTo.Email))
            return;

        if (!await IsNotificationEnabledForUserAsync(workOrder.AssignedToId!.Value, NotificationType.WorkOrderAssigned, NotificationChannel.Email, cancellationToken))
            return;

        var subject = $"Work Order Assigned: {workOrder.WorkOrderNumber}";
        var body = $@"You have been assigned a new work order.

Work Order: {workOrder.WorkOrderNumber}
Title: {workOrder.Title}
Asset: {workOrder.Asset?.Name ?? "N/A"}
Priority: {workOrder.Priority}
Due Date: {workOrder.ScheduledEndDate?.ToString("MM/dd/yyyy") ?? "Not set"}

Please log in to the CMMS system to view the details.";

        var bodyHtml = GenerateHtmlBody(subject, body, workOrderId, "WorkOrder");

        var notification = new NotificationQueue
        {
            Type = NotificationType.WorkOrderAssigned,
            RecipientUserId = workOrder.AssignedToId,
            RecipientEmail = workOrder.AssignedTo.Email,
            Subject = subject,
            Body = body,
            BodyHtml = bodyHtml,
            ReferenceType = "WorkOrder",
            ReferenceId = workOrderId,
            CreatedAt = DateTime.UtcNow
        };

        await QueueNotificationAsync(notification, cancellationToken);
    }

    public async Task QueueWorkOrderDueReminderAsync(int workOrderId, int daysUntilDue, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.WorkOrders.Query()
            .Include(w => w.AssignedTo)
            .Include(w => w.Asset)
            .FirstOrDefaultAsync(w => w.Id == workOrderId, cancellationToken);

        if (workOrder?.AssignedTo == null || string.IsNullOrEmpty(workOrder.AssignedTo.Email))
            return;

        if (!await IsNotificationEnabledForUserAsync(workOrder.AssignedToId!.Value, NotificationType.WorkOrderApproachingDue, NotificationChannel.Email, cancellationToken))
            return;

        var subject = $"Work Order Due Reminder: {workOrder.WorkOrderNumber} (Due in {daysUntilDue} days)";
        var body = $@"This is a reminder that a work order assigned to you is approaching its due date.

Work Order: {workOrder.WorkOrderNumber}
Title: {workOrder.Title}
Asset: {workOrder.Asset?.Name ?? "N/A"}
Due Date: {workOrder.ScheduledEndDate?.ToString("MM/dd/yyyy")}
Days Until Due: {daysUntilDue}

Please log in to the CMMS system to complete this work order.";

        var notification = new NotificationQueue
        {
            Type = NotificationType.WorkOrderApproachingDue,
            RecipientUserId = workOrder.AssignedToId,
            RecipientEmail = workOrder.AssignedTo.Email,
            Subject = subject,
            Body = body,
            BodyHtml = GenerateHtmlBody(subject, body, workOrderId, "WorkOrder"),
            ReferenceType = "WorkOrder",
            ReferenceId = workOrderId,
            CreatedAt = DateTime.UtcNow
        };

        await QueueNotificationAsync(notification, cancellationToken);
    }

    public async Task QueueWorkOrderOverdueAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.WorkOrders.Query()
            .Include(w => w.AssignedTo)
            .Include(w => w.Asset)
            .FirstOrDefaultAsync(w => w.Id == workOrderId, cancellationToken);

        if (workOrder == null)
            return;

        // Notify assigned technician
        if (workOrder.AssignedTo != null && !string.IsNullOrEmpty(workOrder.AssignedTo.Email))
        {
            if (await IsNotificationEnabledForUserAsync(workOrder.AssignedToId!.Value, NotificationType.WorkOrderOverdue, NotificationChannel.Email, cancellationToken))
            {
                var subject = $"OVERDUE Work Order: {workOrder.WorkOrderNumber}";
                var body = $@"This work order is now overdue.

Work Order: {workOrder.WorkOrderNumber}
Title: {workOrder.Title}
Asset: {workOrder.Asset?.Name ?? "N/A"}
Due Date: {workOrder.ScheduledEndDate?.ToString("MM/dd/yyyy")}

Please complete this work order as soon as possible.";

                var notification = new NotificationQueue
                {
                    Type = NotificationType.WorkOrderOverdue,
                    RecipientUserId = workOrder.AssignedToId,
                    RecipientEmail = workOrder.AssignedTo.Email,
                    Subject = subject,
                    Body = body,
                    BodyHtml = GenerateHtmlBody(subject, body, workOrderId, "WorkOrder"),
                    ReferenceType = "WorkOrder",
                    ReferenceId = workOrderId,
                    CreatedAt = DateTime.UtcNow
                };

                await QueueNotificationAsync(notification, cancellationToken);
            }
        }
    }

    public async Task QueueWorkOrderCompletedAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _unitOfWork.WorkOrders.Query()
            .Include(w => w.Asset)
            .FirstOrDefaultAsync(w => w.Id == workOrderId, cancellationToken);

        if (workOrder == null || !workOrder.CreatedBy.HasValue)
            return;

        // Get the creator user
        var createdByUser = await _unitOfWork.Users.GetByIdAsync(workOrder.CreatedBy.Value, cancellationToken);
        if (createdByUser == null || string.IsNullOrEmpty(createdByUser.Email))
            return;

        if (!await IsNotificationEnabledForUserAsync(workOrder.CreatedBy.Value, NotificationType.WorkOrderCompleted, NotificationChannel.Email, cancellationToken))
            return;

        var subject = $"Work Order Completed: {workOrder.WorkOrderNumber}";
        var body = $@"A work order you requested has been completed.

Work Order: {workOrder.WorkOrderNumber}
Title: {workOrder.Title}
Asset: {workOrder.Asset?.Name ?? "N/A"}
Completed: {workOrder.ActualEndDate?.ToString("MM/dd/yyyy HH:mm") ?? DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm")}

Please log in to the CMMS system to view the completion details.";

        var notification = new NotificationQueue
        {
            Type = NotificationType.WorkOrderCompleted,
            RecipientUserId = workOrder.CreatedBy,
            RecipientEmail = createdByUser.Email,
            Subject = subject,
            Body = body,
            BodyHtml = GenerateHtmlBody(subject, body, workOrderId, "WorkOrder"),
            ReferenceType = "WorkOrder",
            ReferenceId = workOrderId,
            CreatedAt = DateTime.UtcNow
        };

        await QueueNotificationAsync(notification, cancellationToken);
    }

    public async Task QueuePMDueReminderAsync(int scheduleId, int daysUntilDue, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(s => s.Asset)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

        if (schedule == null)
            return;

        // Get technicians to notify (all active technicians for now since PM doesn't have a default assignee)
        var technicians = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Technician" || ur.Role.Name == "Admin"))
            .ToListAsync(cancellationToken);

        var subject = $"PM Schedule Due: {schedule.Name} (Due in {daysUntilDue} days)";
        var body = $@"A preventive maintenance schedule is approaching its due date.

Schedule: {schedule.Name}
Asset: {schedule.Asset?.Name ?? "N/A"}
Next Due Date: {schedule.NextDueDate?.ToString("MM/dd/yyyy")}
Days Until Due: {daysUntilDue}

Please prepare for this scheduled maintenance.";

        foreach (var tech in technicians.Where(t => !string.IsNullOrEmpty(t.Email)))
        {
            if (!await IsNotificationEnabledForUserAsync(tech.Id, NotificationType.PMScheduleComingDue, NotificationChannel.Email, cancellationToken))
                continue;

            var notification = new NotificationQueue
            {
                Type = NotificationType.PMScheduleComingDue,
                RecipientUserId = tech.Id,
                RecipientEmail = tech.Email,
                Subject = subject,
                Body = body,
                BodyHtml = GenerateHtmlBody(subject, body, scheduleId, "PM"),
                ReferenceType = "PM",
                ReferenceId = scheduleId,
                CreatedAt = DateTime.UtcNow
            };

            await QueueNotificationAsync(notification, cancellationToken);
        }
    }

    public async Task QueuePMOverdueAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(s => s.Asset)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

        if (schedule == null)
            return;

        // Get technicians/admins to notify
        var technicians = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Technician" || ur.Role.Name == "Admin"))
            .ToListAsync(cancellationToken);

        var subject = $"OVERDUE PM Schedule: {schedule.Name}";
        var body = $@"A preventive maintenance schedule is now overdue.

Schedule: {schedule.Name}
Asset: {schedule.Asset?.Name ?? "N/A"}
Due Date: {schedule.NextDueDate?.ToString("MM/dd/yyyy")}

Please complete this maintenance task as soon as possible.";

        foreach (var tech in technicians.Where(t => !string.IsNullOrEmpty(t.Email)))
        {
            if (!await IsNotificationEnabledForUserAsync(tech.Id, NotificationType.PMScheduleOverdue, NotificationChannel.Email, cancellationToken))
                continue;

            var notification = new NotificationQueue
            {
                Type = NotificationType.PMScheduleOverdue,
                RecipientUserId = tech.Id,
                RecipientEmail = tech.Email,
                Subject = subject,
                Body = body,
                BodyHtml = GenerateHtmlBody(subject, body, scheduleId, "PM"),
                ReferenceType = "PM",
                ReferenceId = scheduleId,
                CreatedAt = DateTime.UtcNow
            };

            await QueueNotificationAsync(notification, cancellationToken);
        }
    }

    public async Task QueueLowStockAlertAsync(int partId, CancellationToken cancellationToken = default)
    {
        var part = await _unitOfWork.Parts.Query()
            .Include(p => p.Stocks)
            .FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part == null)
            return;

        var totalQuantity = part.Stocks?.Sum(s => s.QuantityOnHand) ?? 0;

        // Get inventory managers (users with InventoryManager role)
        var inventoryManagers = await _unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "InventoryManager" || ur.Role.Name == "Admin"))
            .ToListAsync(cancellationToken);

        var subject = $"Low Stock Alert: {part.PartNumber} - {part.Name}";
        var body = $@"A part is running low on stock.

Part Number: {part.PartNumber}
Name: {part.Name}
Current Quantity: {totalQuantity}
Reorder Point: {part.ReorderPoint}
Reorder Quantity: {part.ReorderQuantity}

Please reorder this part.";

        foreach (var manager in inventoryManagers.Where(m => !string.IsNullOrEmpty(m.Email)))
        {
            if (!await IsNotificationEnabledForUserAsync(manager.Id, NotificationType.LowStockAlert, NotificationChannel.Email, cancellationToken))
                continue;

            var notification = new NotificationQueue
            {
                Type = NotificationType.LowStockAlert,
                RecipientUserId = manager.Id,
                RecipientEmail = manager.Email,
                Subject = subject,
                Body = body,
                BodyHtml = GenerateHtmlBody(subject, body, partId, "Part"),
                ReferenceType = "Part",
                ReferenceId = partId,
                CreatedAt = DateTime.UtcNow
            };

            await QueueNotificationAsync(notification, cancellationToken);
        }
    }

    public async Task<NotificationQueue> QueueNotificationAsync(NotificationQueue notification, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.NotificationQueue.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Queued notification {Type} for {Recipient}", notification.Type, notification.RecipientEmail);
        return notification;
    }

    public async Task<int> ProcessPendingNotificationsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var notifications = await GetPendingNotificationsAsync(batchSize, cancellationToken);
        var processedCount = 0;

        foreach (var notification in notifications)
        {
            try
            {
                await MarkAsProcessingAsync(notification.Id, cancellationToken);

                var message = new EmailMessage
                {
                    To = notification.RecipientEmail,
                    Subject = notification.Subject,
                    Body = notification.Body,
                    BodyHtml = notification.BodyHtml
                };

                var result = await _emailProvider.SendEmailAsync(message, cancellationToken);

                if (result.Success)
                {
                    await MarkAsSentAsync(notification.Id, result.MessageId, cancellationToken);
                    await LogNotificationAsync(new NotificationLog
                    {
                        Type = notification.Type,
                        RecipientEmail = notification.RecipientEmail,
                        Subject = notification.Subject,
                        Channel = NotificationChannel.Email,
                        Success = true,
                        ExternalMessageId = result.MessageId,
                        QueueId = notification.Id,
                        ReferenceType = notification.ReferenceType,
                        ReferenceId = notification.ReferenceId,
                        SentAt = DateTime.UtcNow
                    }, cancellationToken);
                    processedCount++;
                }
                else
                {
                    await MarkAsFailedAsync(notification.Id, result.ErrorMessage ?? "Unknown error", cancellationToken);
                    await LogNotificationAsync(new NotificationLog
                    {
                        Type = notification.Type,
                        RecipientEmail = notification.RecipientEmail,
                        Subject = notification.Subject,
                        Channel = NotificationChannel.Email,
                        Success = false,
                        ErrorMessage = result.ErrorMessage,
                        QueueId = notification.Id,
                        ReferenceType = notification.ReferenceType,
                        ReferenceId = notification.ReferenceId,
                        SentAt = DateTime.UtcNow
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
                await MarkAsFailedAsync(notification.Id, ex.Message, cancellationToken);
            }
        }

        return processedCount;
    }

    public async Task<IEnumerable<NotificationQueue>> GetPendingNotificationsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var maxRetries = _settings.Notifications.MaxRetryAttempts;
        var retryDelayMinutes = _settings.Notifications.RetryDelayMinutes;

        return await _unitOfWork.NotificationQueue.Query()
            .Where(n => n.Status == NotificationStatus.Pending
                     && n.ScheduledFor <= DateTime.UtcNow
                     && n.RetryCount < maxRetries)
            .OrderBy(n => n.ScheduledFor)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessingAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.NotificationQueue.GetByIdAsync(notificationId, cancellationToken);
        if (notification != null)
        {
            notification.Status = NotificationStatus.Processing;
            notification.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsSentAsync(int notificationId, string? externalMessageId, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.NotificationQueue.GetByIdAsync(notificationId, cancellationToken);
        if (notification != null)
        {
            notification.Status = NotificationStatus.Sent;
            notification.ProcessedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(int notificationId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.NotificationQueue.GetByIdAsync(notificationId, cancellationToken);
        if (notification != null)
        {
            notification.RetryCount++;
            notification.ErrorMessage = errorMessage;
            notification.UpdatedAt = DateTime.UtcNow;

            if (notification.RetryCount >= _settings.Notifications.MaxRetryAttempts)
            {
                notification.Status = NotificationStatus.Failed;
                notification.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                notification.Status = NotificationStatus.Pending;
                notification.ScheduledFor = DateTime.UtcNow.AddMinutes(_settings.Notifications.RetryDelayMinutes);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsNotificationEnabledForUserAsync(int userId, NotificationType type, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var preference = await _unitOfWork.UserNotificationPreferences.Query()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type, cancellationToken);

        // Default to enabled if no preference exists
        if (preference == null)
            return true;

        return channel switch
        {
            NotificationChannel.Email => preference.EmailEnabled,
            NotificationChannel.Calendar => preference.CalendarEnabled,
            _ => true
        };
    }

    public async Task<IEnumerable<UserNotificationPreference>> GetUserPreferencesAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.UserNotificationPreferences.Query()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserNotificationPreference> SetUserPreferenceAsync(int userId, NotificationType type, bool emailEnabled, bool calendarEnabled, CancellationToken cancellationToken = default)
    {
        var preference = await _unitOfWork.UserNotificationPreferences.Query()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type, cancellationToken);

        if (preference != null)
        {
            preference.EmailEnabled = emailEnabled;
            preference.CalendarEnabled = calendarEnabled;
            preference.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserNotificationPreferences.Update(preference);
        }
        else
        {
            preference = new UserNotificationPreference
            {
                UserId = userId,
                NotificationType = type,
                EmailEnabled = emailEnabled,
                CalendarEnabled = calendarEnabled,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.UserNotificationPreferences.AddAsync(preference, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return preference;
    }

    public async Task LogNotificationAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.NotificationLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationLog>> GetNotificationLogsAsync(int pageSize, int page, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.NotificationLogs.Query()
            .OrderByDescending(l => l.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private static string GenerateHtmlBody(string subject, string plainTextBody, int referenceId, string referenceType)
    {
        var formattedBody = plainTextBody.Replace("\n", "<br/>");
        return $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #1976d2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{subject}</h2>
        </div>
        <div class='content'>
            <p>{formattedBody}</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from CMMS. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }
}
