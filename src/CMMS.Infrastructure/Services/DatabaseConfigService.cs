using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CMMS.Infrastructure.Services;

public class DatabaseConfigService : IDatabaseConfigService
{
    private readonly string _configFilePath;
    private readonly ILogger<DatabaseConfigService> _logger;
    private DatabaseSettings? _cachedSettings;
    private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("CmmsDbConfig2024!@#$%^&*()Key32"); // 32 bytes for AES-256

    public DatabaseConfigService(ILogger<DatabaseConfigService> logger, string? configDirectory = null)
    {
        _logger = logger;

        // Use provided directory or default to app data
        var directory = configDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CMMS");

        Directory.CreateDirectory(directory);
        _configFilePath = Path.Combine(directory, "database.config.json");
    }

    public async Task<DatabaseSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        if (!File.Exists(_configFilePath))
        {
            // Return default settings (not configured)
            return new DatabaseSettings { IsConfigured = false };
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            var settings = JsonSerializer.Deserialize<DatabaseSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (settings != null)
            {
                // Decrypt password
                if (!string.IsNullOrEmpty(settings.Password))
                {
                    settings.Password = Decrypt(settings.Password);
                }

                _cachedSettings = settings;
                return settings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load database configuration");
        }

        return new DatabaseSettings { IsConfigured = false };
    }

    public async Task SaveSettingsAsync(DatabaseSettings settings)
    {
        try
        {
            // Create a copy for saving with encrypted password
            var settingsToSave = new DatabaseSettings
            {
                Provider = settings.Provider,
                Server = settings.Server,
                Port = settings.Port,
                Database = settings.Database,
                AuthType = settings.AuthType,
                Username = settings.Username,
                Password = !string.IsNullOrEmpty(settings.Password) ? Encrypt(settings.Password) : null,
                AdditionalOptions = settings.AdditionalOptions,
                FilePath = settings.FilePath,
                IsConfigured = settings.IsConfigured,
                Tier = settings.Tier
            };

            var json = JsonSerializer.Serialize(settingsToSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_configFilePath, json);

            // Update cache with unencrypted version
            _cachedSettings = settings;

            _logger.LogInformation("Database configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save database configuration");
            throw;
        }
    }

    public async Task<DatabaseTestResult> TestConnectionAsync(DatabaseSettings settings)
    {
        var result = new DatabaseTestResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var connectionString = settings.BuildConnectionString();

            switch (settings.Provider)
            {
                case DatabaseProvider.SqlServer:
                    result = await TestSqlServerAsync(connectionString);
                    break;

                case DatabaseProvider.Sqlite:
                    result = await TestSqliteAsync(connectionString, settings.FilePath);
                    break;

                case DatabaseProvider.PostgreSql:
                    result = TestNotYetSupported("PostgreSQL");
                    break;

                case DatabaseProvider.MySql:
                    result = TestNotYetSupported("MySQL");
                    break;

                default:
                    result.Success = false;
                    result.Message = "Unknown database provider";
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Connection failed";
            result.ErrorDetails = ex.Message;
            _logger.LogError(ex, "Database connection test failed");
        }

        stopwatch.Stop();
        result.LatencyMs = (int)stopwatch.ElapsedMilliseconds;

        return result;
    }

    private async Task<DatabaseTestResult> TestSqlServerAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@VERSION";
        var version = await command.ExecuteScalarAsync();

        return new DatabaseTestResult
        {
            Success = true,
            Message = "Connection successful",
            ServerVersion = version?.ToString()?.Split('\n').FirstOrDefault()
        };
    }

    private async Task<DatabaseTestResult> TestSqliteAsync(string connectionString, string? filePath)
    {
        // For SQLite, we just verify we can create/open the file
        var path = filePath ?? "CMMS.db";
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Test with Microsoft.Data.Sqlite
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version()";
        var version = await command.ExecuteScalarAsync();

        return new DatabaseTestResult
        {
            Success = true,
            Message = "Connection successful",
            ServerVersion = $"SQLite {version}"
        };
    }

    private DatabaseTestResult TestNotYetSupported(string providerName)
    {
        return new DatabaseTestResult
        {
            Success = false,
            Message = $"{providerName} support is planned for a future release",
            ErrorDetails = "This database provider is not yet implemented"
        };
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.IsConfigured;
    }

    public string GetConnectionString()
    {
        if (_cachedSettings == null)
        {
            // Load synchronously for startup
            var settings = GetSettingsAsync().GetAwaiter().GetResult();
            return settings.BuildConnectionString();
        }

        return _cachedSettings.BuildConnectionString();
    }

    public DatabaseProvider GetProvider()
    {
        if (_cachedSettings == null)
        {
            var settings = GetSettingsAsync().GetAwaiter().GetResult();
            return settings.Provider;
        }

        return _cachedSettings.Provider;
    }

    private static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combine IV and encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private static string Decrypt(string cipherText)
    {
        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = EncryptionKey;

            // Extract IV from beginning
            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // If decryption fails, return as-is (might be unencrypted)
            return cipherText;
        }
    }
}
