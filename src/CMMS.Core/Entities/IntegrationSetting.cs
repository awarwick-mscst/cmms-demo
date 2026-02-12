namespace CMMS.Core.Entities;

public class IntegrationSetting : BaseEntity
{
    public string ProviderType { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
