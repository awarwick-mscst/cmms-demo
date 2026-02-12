using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IIntegrationSettingsService
{
    // Settings CRUD
    Task<string?> GetSettingAsync(string providerType, string settingKey, CancellationToken cancellationToken = default);
    Task SetSettingAsync(string providerType, string settingKey, string value, DateTime? expiresAt = null, CancellationToken cancellationToken = default);
    Task DeleteSettingAsync(string providerType, string settingKey, CancellationToken cancellationToken = default);

    // Provider-specific settings
    Task<Dictionary<string, string>> GetProviderSettingsAsync(string providerType, CancellationToken cancellationToken = default);
    Task SetProviderSettingsAsync(string providerType, Dictionary<string, string> settings, CancellationToken cancellationToken = default);
    Task DeleteProviderSettingsAsync(string providerType, CancellationToken cancellationToken = default);

    // Provider availability
    Task<bool> IsProviderConfiguredAsync(string providerType, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetConfiguredProvidersAsync(CancellationToken cancellationToken = default);
}
