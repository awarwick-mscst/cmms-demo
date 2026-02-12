using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace CMMS.API.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailCalendarSettings _settings;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<EmailCalendarSettings> settings,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationBackgroundService starting");

        if (!_settings.Enabled)
        {
            _logger.LogInformation("Notification system is disabled, background service will not process notifications");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification background service");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.Notifications.ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("NotificationBackgroundService stopping");
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var processedCount = await notificationService.ProcessPendingNotificationsAsync(
            _settings.Notifications.BatchSize,
            cancellationToken);

        if (processedCount > 0)
        {
            _logger.LogInformation("Processed {Count} notifications", processedCount);
        }
    }
}
