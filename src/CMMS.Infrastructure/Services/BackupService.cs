using System.Text.Json;
using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using CMMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMMS.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly CmmsDbContext _context;
    private readonly ILogger<BackupService> _logger;

    // Tables in dependency order (referenced tables first)
    private static readonly string[] ExportOrder = new[]
    {
        // Core - no dependencies
        "roles",
        "permissions",

        // Users - depends on roles
        "users",
        "user_roles",
        "role_permissions",
        "refresh_tokens",

        // Asset hierarchy
        "asset_categories",
        "asset_locations",
        "assets",
        "asset_documents",

        // Inventory hierarchy
        "suppliers",
        "part_categories",
        "storage_locations",
        "parts",
        "part_stocks",
        "part_transactions",

        // Work order hierarchy
        "work_order_task_templates",
        "work_order_task_template_items",
        "preventive_maintenance_schedules",
        "work_orders",
        "work_order_tasks",
        "work_order_comments",
        "work_order_labor",
        "work_order_history",
        "work_sessions",
        "asset_parts",

        // Admin
        "label_templates",
        "label_printers",

        // Attachments (metadata only, files stored separately)
        "attachments",
    };

    public BackupService(CmmsDbContext context, ILogger<BackupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BackupExportResult> CreateExportAsync(CancellationToken cancellationToken = default)
    {
        var result = new BackupExportResult();

        try
        {
            var backupData = new BackupData
            {
                Metadata = new BackupMetadata
                {
                    Version = "1.0",
                    ExportedAt = DateTime.UtcNow,
                    AppVersion = "1.0.0",
                    SourceDatabase = SqlDialect.Provider == DatabaseProvider.PostgreSql ? "PostgreSQL" : "MSSQL",
                    RecordCounts = new Dictionary<string, int>()
                },
                Schema = new BackupSchema
                {
                    Tables = ExportOrder.ToList(),
                    ImportOrder = ExportOrder.ToList()
                },
                Data = new Dictionary<string, List<Dictionary<string, object?>>>()
            };

            foreach (var tableName in ExportOrder)
            {
                try
                {
                    var tableData = await ExportTableAsync(tableName, cancellationToken);
                    backupData.Data[tableName] = tableData;
                    backupData.Metadata.RecordCounts[tableName] = tableData.Count;
                    _logger.LogInformation("Exported {Count} records from {Table}", tableData.Count, tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not export table {Table}, it may not exist", tableName);
                    // Continue with other tables - some tables may not exist
                }
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(backupData, jsonOptions);

            result.Success = true;
            result.FileName = $"cmms-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            result.Data = jsonBytes;
            result.Metadata = backupData.Metadata;

            _logger.LogInformation("Backup created successfully: {FileName}, {Size} bytes",
                result.FileName, jsonBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            result.Success = false;
            result.Errors.Add($"Failed to create backup: {ex.Message}");
        }

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> ExportTableAsync(
        string tableName,
        CancellationToken cancellationToken)
    {
        var schemaTable = GetSchemaAndTable(tableName);
        var sql = $"SELECT * FROM {SqlDialect.QuoteSchemaTable(schemaTable.schema, schemaTable.table)}";

        var results = new List<Dictionary<string, object?>>();

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i))
                .ToList();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                foreach (var column in columns)
                {
                    var value = reader[column];
                    row[column] = value == DBNull.Value ? null : ConvertToPortableValue(value);
                }
                results.Add(row);
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        return results;
    }

    private static object? ConvertToPortableValue(object value)
    {
        return value switch
        {
            DateTime dt => dt.ToString("O"), // ISO 8601 format
            DateTimeOffset dto => dto.ToString("O"),
            byte[] bytes => Convert.ToBase64String(bytes),
            decimal d => d,
            _ => value
        };
    }

    private static (string schema, string table) GetSchemaAndTable(string tableName)
    {
        // Map table names to their schemas
        return tableName switch
        {
            "roles" or "permissions" or "users" or "user_roles" or "role_permissions"
                or "refresh_tokens" or "audit_logs" or "attachments"
                => ("core", tableName),

            "asset_categories" or "asset_locations" or "assets" or "asset_documents"
                => ("assets", tableName),

            "suppliers" or "part_categories" or "storage_locations" or "parts"
                or "part_stocks" or "part_transactions" or "asset_parts"
                => ("inventory", tableName),

            "work_orders" or "work_order_tasks" or "work_order_comments"
                or "work_order_labor" or "work_order_history" or "work_sessions"
                or "work_order_task_templates" or "work_order_task_template_items"
                or "preventive_maintenance_schedules"
                => ("maintenance", tableName),

            "label_templates" or "label_printers"
                => ("admin", tableName),

            _ => ("dbo", tableName)
        };
    }

    public async Task<BackupImportResult> ImportAsync(
        BackupData backupData,
        bool clearExisting = false,
        CancellationToken cancellationToken = default)
    {
        var result = new BackupImportResult();

        try
        {
            // Validate first
            var validation = await ValidateBackupAsync(backupData, cancellationToken);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.Errors.AddRange(validation.Errors);
                return result;
            }

            // Use the import order from the backup, or default order
            var importOrder = backupData.Schema.ImportOrder.Any()
                ? backupData.Schema.ImportOrder
                : ExportOrder.ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (clearExisting)
                {
                    // Delete in reverse order to respect foreign keys
                    foreach (var tableName in importOrder.AsEnumerable().Reverse())
                    {
                        if (backupData.Data.ContainsKey(tableName))
                        {
                            await ClearTableAsync(tableName, cancellationToken);
                        }
                    }
                }

                // Enable identity insert and import data
                foreach (var tableName in importOrder)
                {
                    if (backupData.Data.TryGetValue(tableName, out var tableData) && tableData.Any())
                    {
                        var imported = await ImportTableAsync(tableName, tableData, cancellationToken);
                        result.RecordsImported += imported;
                        result.TablesImported++;
                        _logger.LogInformation("Imported {Count} records into {Table}", imported, tableName);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                result.Success = true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import backup");
            result.Success = false;
            result.Errors.Add($"Failed to import backup: {ex.Message}");
        }

        return result;
    }

    private async Task ClearTableAsync(string tableName, CancellationToken cancellationToken)
    {
        var schemaTable = GetSchemaAndTable(tableName);
        var sql = $"DELETE FROM {SqlDialect.QuoteSchemaTable(schemaTable.schema, schemaTable.table)}";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task<int> ImportTableAsync(
        string tableName,
        List<Dictionary<string, object?>> data,
        CancellationToken cancellationToken)
    {
        if (!data.Any()) return 0;

        var schemaTable = GetSchemaAndTable(tableName);
        var fullTableName = SqlDialect.QuoteSchemaTable(schemaTable.schema, schemaTable.table);

        // Check if table has identity/serial column
        var hasIdentity = await HasIdentityColumnAsync(schemaTable.schema, schemaTable.table, cancellationToken);

        var count = 0;

        try
        {
            if (hasIdentity && SqlDialect.Provider != DatabaseProvider.PostgreSql)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    $"SET IDENTITY_INSERT {fullTableName} ON", cancellationToken);
            }

            foreach (var row in data)
            {
                var columns = row.Keys.ToList();
                var columnList = string.Join(", ", columns.Select(c => SqlDialect.QuoteIdentifier(c)));
                var paramList = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

                var sql = $"INSERT INTO {fullTableName} ({columnList}) VALUES ({paramList})";

                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;

                for (int i = 0; i < columns.Count; i++)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = $"@p{i}";
                    param.Value = row[columns[i]] ?? DBNull.Value;
                    command.Parameters.Add(param);
                }

                await _context.Database.OpenConnectionAsync(cancellationToken);
                await command.ExecuteNonQueryAsync(cancellationToken);
                count++;
            }

            // For PostgreSQL, reset sequences after bulk insert
            if (hasIdentity && SqlDialect.Provider == DatabaseProvider.PostgreSql)
            {
                var resetSql = $@"
                    SELECT setval(pg_get_serial_sequence('{schemaTable.schema}.{schemaTable.table}', 'id'),
                           COALESCE((SELECT MAX(id) FROM {fullTableName}), 0) + 1, false)";
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(resetSql, cancellationToken);
                }
                catch
                {
                    // Sequence may not exist if column uses GENERATED ALWAYS
                }
            }
        }
        finally
        {
            if (hasIdentity && SqlDialect.Provider != DatabaseProvider.PostgreSql)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    $"SET IDENTITY_INSERT {fullTableName} OFF", cancellationToken);
            }
        }

        return count;
    }

    private async Task<bool> HasIdentityColumnAsync(string schema, string table, CancellationToken cancellationToken)
    {
        string sql;
        if (SqlDialect.Provider == DatabaseProvider.PostgreSql)
        {
            sql = @"
                SELECT COUNT(*)
                FROM information_schema.columns
                WHERE table_schema = @schema AND table_name = @table
                  AND (column_default LIKE 'nextval%' OR is_identity = 'YES')";
        }
        else
        {
            sql = @"
                SELECT COUNT(*)
                FROM sys.columns c
                JOIN sys.tables t ON c.object_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE s.name = @schema AND t.name = @table AND c.is_identity = 1";
        }

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var schemaParam = command.CreateParameter();
        schemaParam.ParameterName = "@schema";
        schemaParam.Value = schema;
        command.Parameters.Add(schemaParam);

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@table";
        tableParam.Value = table;
        command.Parameters.Add(tableParam);

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public Task<BackupValidationResult> ValidateBackupAsync(
        BackupData backupData,
        CancellationToken cancellationToken = default)
    {
        var result = new BackupValidationResult
        {
            Version = backupData.Metadata?.Version,
            ExportedAt = backupData.Metadata?.ExportedAt,
            Tables = backupData.Data?.Keys.ToList() ?? new List<string>(),
            RecordCounts = backupData.Metadata?.RecordCounts ?? new Dictionary<string, int>()
        };

        // Validate metadata
        if (backupData.Metadata == null)
        {
            result.Errors.Add("Missing backup metadata");
        }
        else if (string.IsNullOrEmpty(backupData.Metadata.Version))
        {
            result.Errors.Add("Missing backup version");
        }

        // Validate data
        if (backupData.Data == null || !backupData.Data.Any())
        {
            result.Errors.Add("No data found in backup");
        }

        // Check for required tables
        var requiredTables = new[] { "users", "roles" };
        foreach (var table in requiredTables)
        {
            if (backupData.Data == null || !backupData.Data.ContainsKey(table))
            {
                result.Warnings.Add($"Missing recommended table: {table}");
            }
        }

        result.IsValid = !result.Errors.Any();
        return Task.FromResult(result);
    }
}
