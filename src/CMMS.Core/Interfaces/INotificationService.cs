using CMMS.Core.Entities;
using CMMS.Core.Enums;

namespace CMMS.Core.Interfaces;

public interface INotificationService
{
    // Queue notification methods
    Task QueueWorkOrderAssignedAsync(int workOrderId, CancellationToken cancellationToken = default);
    Task QueueWorkOrderDueReminderAsync(int workOrderId, int daysUntilDue, CancellationToken cancellationToken = default);
    Task QueueWorkOrderOverdueAsync(int workOrderId, CancellationToken cancellationToken = default);
    Task QueueWorkOrderCompletedAsync(int workOrderId, CancellationToken cancellationToken = default);
    Task QueuePMDueReminderAsync(int scheduleId, int daysUntilDue, CancellationToken cancellationToken = default);
    Task QueuePMOverdueAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task QueueLowStockAlertAsync(int partId, CancellationToken cancellationToken = default);

    // Queue management
    Task<NotificationQueue> QueueNotificationAsync(NotificationQueue notification, CancellationToken cancellationToken = default);
    Task<int> ProcessPendingNotificationsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationQueue>> GetPendingNotificationsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkAsProcessingAsync(int notificationId, CancellationToken cancellationToken = default);
    Task MarkAsSentAsync(int notificationId, string? externalMessageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(int notificationId, string errorMessage, CancellationToken cancellationToken = default);

    // User preference checks
    Task<bool> IsNotificationEnabledForUserAsync(int userId, NotificationType type, NotificationChannel channel, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserNotificationPreference>> GetUserPreferencesAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserNotificationPreference> SetUserPreferenceAsync(int userId, NotificationType type, bool emailEnabled, bool calendarEnabled, CancellationToken cancellationToken = default);

    // Logging
    Task LogNotificationAsync(NotificationLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationLog>> GetNotificationLogsAsync(int pageSize, int page, CancellationToken cancellationToken = default);
}
