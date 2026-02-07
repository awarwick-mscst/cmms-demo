namespace CMMS.Core.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";

    /// <summary>
    /// The type of database server
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    /// <summary>
    /// Database server hostname or IP address
    /// </summary>
    public string Server { get; set; } = "localhost";

    /// <summary>
    /// Database server port (optional, uses default if not specified)
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    public string Database { get; set; } = "CMMS";

    /// <summary>
    /// Authentication type
    /// </summary>
    public DatabaseAuthType AuthType { get; set; } = DatabaseAuthType.Windows;

    /// <summary>
    /// Username for SQL authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for SQL authentication (encrypted at rest)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Additional connection string options
    /// </summary>
    public string? AdditionalOptions { get; set; }

    /// <summary>
    /// For SQLite: the file path
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Whether the database has been configured
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Deployment tier
    /// </summary>
    public DeploymentTier Tier { get; set; } = DeploymentTier.Small;

    /// <summary>
    /// Builds the connection string based on the settings
    /// </summary>
    public string BuildConnectionString()
    {
        return Provider switch
        {
            DatabaseProvider.SqlServer => BuildSqlServerConnectionString(),
            DatabaseProvider.PostgreSql => BuildPostgreSqlConnectionString(),
            DatabaseProvider.MySql => BuildMySqlConnectionString(),
            DatabaseProvider.Sqlite => BuildSqliteConnectionString(),
            _ => throw new NotSupportedException($"Database provider {Provider} is not supported")
        };
    }

    private string BuildSqlServerConnectionString()
    {
        var server = Port.HasValue ? $"{Server},{Port}" : Server;
        var parts = new List<string>
        {
            $"Data Source={server}",
            $"Initial Catalog={Database}",
            "TrustServerCertificate=true",
            "MultipleActiveResultSets=true"
        };

        if (AuthType == DatabaseAuthType.Windows)
        {
            parts.Add("Integrated Security=true");
        }
        else
        {
            parts.Add($"User ID={Username}");
            parts.Add($"Password={Password}");
        }

        if (!string.IsNullOrEmpty(AdditionalOptions))
        {
            parts.Add(AdditionalOptions);
        }

        return string.Join(";", parts);
    }

    private string BuildPostgreSqlConnectionString()
    {
        var port = Port ?? 5432;
        var cs = $"Host={Server};Port={port};Database={Database};";

        if (AuthType == DatabaseAuthType.SqlAuth)
        {
            cs += $"Username={Username};Password={Password};";
        }

        if (!string.IsNullOrEmpty(AdditionalOptions))
        {
            cs += AdditionalOptions;
        }

        return cs;
    }

    private string BuildMySqlConnectionString()
    {
        var port = Port ?? 3306;
        var cs = $"Server={Server};Port={port};Database={Database};";

        if (AuthType == DatabaseAuthType.SqlAuth)
        {
            cs += $"User={Username};Password={Password};";
        }

        if (!string.IsNullOrEmpty(AdditionalOptions))
        {
            cs += AdditionalOptions;
        }

        return cs;
    }

    private string BuildSqliteConnectionString()
    {
        var path = FilePath ?? $"{Database}.db";
        return $"Data Source={path}";
    }

    /// <summary>
    /// Gets the default port for the provider
    /// </summary>
    public static int GetDefaultPort(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer => 1433,
            DatabaseProvider.PostgreSql => 5432,
            DatabaseProvider.MySql => 3306,
            DatabaseProvider.Sqlite => 0,
            _ => 0
        };
    }
}

public enum DatabaseProvider
{
    SqlServer,
    PostgreSql,
    MySql,
    Sqlite
}

public enum DatabaseAuthType
{
    Windows,
    SqlAuth
}

public enum DeploymentTier
{
    /// <summary>
    /// Single workstation, SQLite or SQL Express, for very small businesses
    /// </summary>
    Tiny,

    /// <summary>
    /// Single server, SQL Server Express or Standard
    /// </summary>
    Small,

    /// <summary>
    /// Separate DB and App servers, SQL Server Standard/Enterprise
    /// </summary>
    Enterprise,

    /// <summary>
    /// Azure hosted, Azure SQL, Entra ID auth
    /// </summary>
    Cloud
}
