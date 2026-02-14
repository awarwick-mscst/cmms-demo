using CMMS.Core.Configuration;

namespace CMMS.Infrastructure.Data;

/// <summary>
/// Provides provider-specific SQL fragments for entity configurations.
/// Must be initialized via <see cref="Provider"/> before any DbContext is created.
/// </summary>
public static class SqlDialect
{
    private static DatabaseProvider _provider = DatabaseProvider.SqlServer;
    private static bool _initialized;

    public static DatabaseProvider Provider
    {
        get => _provider;
        set
        {
            _provider = value;
            _initialized = true;
        }
    }

    public static bool IsInitialized => _initialized;

    /// <summary>GETUTCDATE() or NOW() AT TIME ZONE 'UTC'</summary>
    public static string UtcNow() => _provider switch
    {
        DatabaseProvider.PostgreSql => "NOW() AT TIME ZONE 'UTC'",
        _ => "GETUTCDATE()"
    };

    /// <summary>nvarchar(max) or text</summary>
    public static string UnboundedText() => _provider switch
    {
        DatabaseProvider.PostgreSql => "text",
        _ => "nvarchar(max)"
    };

    /// <summary>[is_deleted] = 0 or "is_deleted" = false</summary>
    public static string SoftDeleteFilter() => _provider switch
    {
        DatabaseProvider.PostgreSql => "\"is_deleted\" = false",
        _ => "[is_deleted] = 0"
    };

    /// <summary>Soft-delete filter combined with a NOT NULL check on the given column.</summary>
    public static string SoftDeleteAndNotNullFilter(string column) => _provider switch
    {
        DatabaseProvider.PostgreSql => $"\"is_deleted\" = false AND \"{column}\" IS NOT NULL",
        _ => $"[is_deleted] = 0 AND [{column}] IS NOT NULL"
    };

    /// <summary>[col] = 1 or "col" = true</summary>
    public static string BooleanTrueFilter(string column) => _provider switch
    {
        DatabaseProvider.PostgreSql => $"\"{column}\" = true",
        _ => $"[{column}] = 1"
    };

    /// <summary>[name] or "name"</summary>
    public static string QuoteIdentifier(string name) => _provider switch
    {
        DatabaseProvider.PostgreSql => $"\"{name}\"",
        _ => $"[{name}]"
    };

    /// <summary>[schema].[table] or "schema"."table"</summary>
    public static string QuoteSchemaTable(string schema, string table) => _provider switch
    {
        DatabaseProvider.PostgreSql => $"\"{schema}\".\"{table}\"",
        _ => $"[{schema}].[{table}]"
    };
}
