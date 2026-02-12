using CMMS.Core.Configuration;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CMMS.API.Services;

public class NotificationSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailCalendarSettings _settings;
    private readonly ILogger<NotificationSchedulerService> _logger;

    public NotificationSchedulerService(
        IServiceProvider serviceProvider,
        IOptions<EmailCalendarSettings> settings,
        ILogger<NotificationSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationSchedulerService starting");

        if (!_settings.Enabled)
        {
            _logger.LogInformation("Notification system is disabled, scheduler will not run");
            return;
        }

        // Run scheduler once per hour
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDueDatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification scheduler service");
            }

            // Wait for 1 hour before next check
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("NotificationSchedulerService stopping");
    }

    private async Task CheckDueDatesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await CheckWorkOrderDueDatesAsync(unitOfWork, notificationService, cancellationToken);
        await CheckPMScheduleDueDatesAsync(unitOfWork, notificationService, cancellationToken);
        await CheckLowStockAsync(unitOfWork, notificationService, cancellationToken);
    }

    private async Task CheckWorkOrderDueDatesAsync(IUnitOfWork unitOfWork, INotificationService notificationService, CancellationToken cancellationToken)
    {
        var reminderDays = _settings.Notifications.WorkOrderDueReminderDays;
        var reminderDate = DateTime.UtcNow.AddDays(reminderDays).Date;
        var today = DateTime.UtcNow.Date;

        // Get work orders approaching due date
        var approachingDue = await unitOfWork.WorkOrders.Query()
            .Where(w => w.Status != WorkOrderStatus.Completed
                     && w.Status != WorkOrderStatus.Cancelled
                     && w.ScheduledEndDate.HasValue
                     && w.ScheduledEndDate.Value.Date == reminderDate
                     && w.AssignedToId.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var workOrder in approachingDue)
        {
            // Check if we already sent a reminder for this work order today
            var alreadySent = await unitOfWork.NotificationQueue.Query()
                .AnyAsync(n => n.ReferenceType == "WorkOrder"
                            && n.ReferenceId == workOrder.Id
                            && n.Type == NotificationType.WorkOrderApproachingDue
                            && n.CreatedAt.Date == today,
                    cancellationToken);

            if (!alreadySent)
            {
                await notificationService.QueueWorkOrderDueReminderAsync(workOrder.Id, reminderDays, cancellationToken);
            }
        }

        // Get overdue work orders
        var overdue = await unitOfWork.WorkOrders.Query()
            .Where(w => w.Status != WorkOrderStatus.Completed
                     && w.Status != WorkOrderStatus.Cancelled
                     && w.ScheduledEndDate.HasValue
                     && w.ScheduledEndDate.Value.Date < today
                     && w.AssignedToId.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var workOrder in overdue)
        {
            // Only send overdue notification once per day
            var alreadySent = await unitOfWork.NotificationQueue.Query()
                .AnyAsync(n => n.ReferenceType == "WorkOrder"
                            && n.ReferenceId == workOrder.Id
                            && n.Type == NotificationType.WorkOrderOverdue
                            && n.CreatedAt.Date == today,
                    cancellationToken);

            if (!alreadySent)
            {
                await notificationService.QueueWorkOrderOverdueAsync(workOrder.Id, cancellationToken);
            }
        }

        _logger.LogInformation("Checked {ApproachingCount} approaching and {OverdueCount} overdue work orders",
            approachingDue.Count, overdue.Count);
    }

    private async Task CheckPMScheduleDueDatesAsync(IUnitOfWork unitOfWork, INotificationService notificationService, CancellationToken cancellationToken)
    {
        var reminderDays = _settings.Notifications.PMDueReminderDays;
        var reminderDate = DateTime.UtcNow.AddDays(reminderDays).Date;
        var today = DateTime.UtcNow.Date;

        // Get PM schedules approaching due date
        var approachingDue = await unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Where(s => s.IsActive
                     && s.NextDueDate.HasValue
                     && s.NextDueDate.Value.Date == reminderDate)
            .ToListAsync(cancellationToken);

        foreach (var schedule in approachingDue)
        {
            var alreadySent = await unitOfWork.NotificationQueue.Query()
                .AnyAsync(n => n.ReferenceType == "PM"
                            && n.ReferenceId == schedule.Id
                            && n.Type == NotificationType.PMScheduleComingDue
                            && n.CreatedAt.Date == today,
                    cancellationToken);

            if (!alreadySent)
            {
                await notificationService.QueuePMDueReminderAsync(schedule.Id, reminderDays, cancellationToken);
            }
        }

        // Get overdue PM schedules
        var overdue = await unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Where(s => s.IsActive
                     && s.NextDueDate.HasValue
                     && s.NextDueDate.Value.Date < today)
            .ToListAsync(cancellationToken);

        foreach (var schedule in overdue)
        {
            var alreadySent = await unitOfWork.NotificationQueue.Query()
                .AnyAsync(n => n.ReferenceType == "PM"
                            && n.ReferenceId == schedule.Id
                            && n.Type == NotificationType.PMScheduleOverdue
                            && n.CreatedAt.Date == today,
                    cancellationToken);

            if (!alreadySent)
            {
                await notificationService.QueuePMOverdueAsync(schedule.Id, cancellationToken);
            }
        }

        _logger.LogInformation("Checked {ApproachingCount} approaching and {OverdueCount} overdue PM schedules",
            approachingDue.Count, overdue.Count);
    }

    private async Task CheckLowStockAsync(IUnitOfWork unitOfWork, INotificationService notificationService, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        // Get parts below reorder point
        var lowStockParts = await unitOfWork.Parts.Query()
            .Include(p => p.Stocks)
            .Where(p => p.ReorderPoint > 0)
            .ToListAsync(cancellationToken);

        var alertCount = 0;
        foreach (var part in lowStockParts)
        {
            var totalQuantity = part.Stocks?.Sum(s => s.QuantityOnHand) ?? 0;
            if (totalQuantity < part.ReorderPoint)
            {
                // Only send one alert per part per day
                var alreadySent = await unitOfWork.NotificationQueue.Query()
                    .AnyAsync(n => n.ReferenceType == "Part"
                                && n.ReferenceId == part.Id
                                && n.Type == NotificationType.LowStockAlert
                                && n.CreatedAt.Date == today,
                        cancellationToken);

                if (!alreadySent)
                {
                    await notificationService.QueueLowStockAlertAsync(part.Id, cancellationToken);
                    alertCount++;
                }
            }
        }

        if (alertCount > 0)
        {
            _logger.LogInformation("Queued {Count} low stock alerts", alertCount);
        }
    }
}
