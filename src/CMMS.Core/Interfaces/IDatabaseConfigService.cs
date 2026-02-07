using CMMS.Core.Configuration;

namespace CMMS.Core.Interfaces;

public interface IDatabaseConfigService
{
    /// <summary>
    /// Gets the current database settings
    /// </summary>
    Task<DatabaseSettings> GetSettingsAsync();

    /// <summary>
    /// Saves database settings
    /// </summary>
    Task SaveSettingsAsync(DatabaseSettings settings);

    /// <summary>
    /// Tests a database connection with the given settings
    /// </summary>
    Task<DatabaseTestResult> TestConnectionAsync(DatabaseSettings settings);

    /// <summary>
    /// Checks if the database has been configured
    /// </summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// Gets the current connection string
    /// </summary>
    string GetConnectionString();

    /// <summary>
    /// Gets the current database provider
    /// </summary>
    DatabaseProvider GetProvider();
}

public class DatabaseTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ServerVersion { get; set; }
    public string? ErrorDetails { get; set; }
    public int? LatencyMs { get; set; }
}
