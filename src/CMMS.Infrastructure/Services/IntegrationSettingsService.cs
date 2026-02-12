using System.Security.Cryptography;
using System.Text;
using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CMMS.Infrastructure.Services;

public class IntegrationSettingsService : IIntegrationSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly byte[] _encryptionKey;

    public IntegrationSettingsService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;

        // Use the JWT secret as the base for encryption key, or a dedicated key
        var keySource = configuration["Jwt:Secret"] ?? configuration["EncryptionKey"] ?? "DefaultEncryptionKey123456789012";
        _encryptionKey = DeriveKey(keySource);
    }

    private static byte[] DeriveKey(string keySource)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
    }

    public async Task<string?> GetSettingAsync(string providerType, string settingKey, CancellationToken cancellationToken = default)
    {
        var setting = await _unitOfWork.IntegrationSettings.Query()
            .FirstOrDefaultAsync(s => s.ProviderType == providerType && s.SettingKey == settingKey, cancellationToken);

        if (setting == null)
            return null;

        // Check expiration
        if (setting.ExpiresAt.HasValue && setting.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        return Decrypt(setting.EncryptedValue);
    }

    public async Task SetSettingAsync(string providerType, string settingKey, string value, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.IntegrationSettings.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.ProviderType == providerType && s.SettingKey == settingKey, cancellationToken);

        var encryptedValue = Encrypt(value);

        if (existing != null)
        {
            existing.EncryptedValue = encryptedValue;
            existing.ExpiresAt = expiresAt;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            _unitOfWork.IntegrationSettings.Update(existing);
        }
        else
        {
            var setting = new IntegrationSetting
            {
                ProviderType = providerType,
                SettingKey = settingKey,
                EncryptedValue = encryptedValue,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.IntegrationSettings.AddAsync(setting, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSettingAsync(string providerType, string settingKey, CancellationToken cancellationToken = default)
    {
        var setting = await _unitOfWork.IntegrationSettings.Query()
            .FirstOrDefaultAsync(s => s.ProviderType == providerType && s.SettingKey == settingKey, cancellationToken);

        if (setting != null)
        {
            setting.IsDeleted = true;
            setting.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Dictionary<string, string>> GetProviderSettingsAsync(string providerType, CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.IntegrationSettings.Query()
            .Where(s => s.ProviderType == providerType)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, string>();
        foreach (var setting in settings)
        {
            // Skip expired settings
            if (setting.ExpiresAt.HasValue && setting.ExpiresAt.Value < DateTime.UtcNow)
                continue;

            try
            {
                result[setting.SettingKey] = Decrypt(setting.EncryptedValue);
            }
            catch
            {
                // Skip settings that can't be decrypted
            }
        }

        return result;
    }

    public async Task SetProviderSettingsAsync(string providerType, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in settings)
        {
            await SetSettingAsync(providerType, kvp.Key, kvp.Value, null, cancellationToken);
        }
    }

    public async Task DeleteProviderSettingsAsync(string providerType, CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.IntegrationSettings.Query()
            .Where(s => s.ProviderType == providerType)
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            setting.IsDeleted = true;
            setting.DeletedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsProviderConfiguredAsync(string providerType, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.IntegrationSettings.Query()
            .AnyAsync(s => s.ProviderType == providerType, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetConfiguredProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.IntegrationSettings.Query()
            .Select(s => s.ProviderType)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string encryptedText)
    {
        var fullData = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        // Extract IV from beginning
        var iv = new byte[16];
        Buffer.BlockCopy(fullData, 0, iv, 0, 16);
        aes.IV = iv;

        // Extract encrypted data
        var encryptedBytes = new byte[fullData.Length - 16];
        Buffer.BlockCopy(fullData, 16, encryptedBytes, 0, encryptedBytes.Length);

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
