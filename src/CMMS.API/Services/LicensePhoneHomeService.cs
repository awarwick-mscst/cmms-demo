using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace CMMS.API.Services;

public class LicensePhoneHomeService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LicensingSettings _settings;
    private readonly ILogger<LicensePhoneHomeService> _logger;

    public LicensePhoneHomeService(
        IServiceProvider serviceProvider,
        IOptions<LicensingSettings> settings,
        ILogger<LicensePhoneHomeService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LicensePhoneHomeService starting");

        if (!_settings.Enabled)
        {
            _logger.LogInformation("Licensing is disabled, phone-home service will not run");
            return;
        }

        // Wait a bit before first phone-home to let the app start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();
                var result = await licenseService.PhoneHomeAsync(stoppingToken);

                if (result.Success)
                {
                    if (result.Warning != null)
                    {
                        _logger.LogWarning("License phone-home warning: {Warning}", result.Warning);
                    }
                    else
                    {
                        _logger.LogDebug("License phone-home successful. Days until expiry: {Days}", result.DaysUntilExpiry);
                    }
                }
                else
                {
                    _logger.LogWarning("License phone-home failed: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in license phone-home service");
            }

            await Task.Delay(TimeSpan.FromHours(_settings.PhoneHomeIntervalHours), stoppingToken);
        }

        _logger.LogInformation("LicensePhoneHomeService stopping");
    }
}
