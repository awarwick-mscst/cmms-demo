namespace CMMS.Core.Interfaces;

public interface IBackupService
{
    /// <summary>
    /// Creates a full database export in a database-agnostic JSON format
    /// </summary>
    Task<BackupExportResult> CreateExportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports data from a backup file
    /// </summary>
    Task<BackupImportResult> ImportAsync(BackupData backupData, bool clearExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a backup file without importing
    /// </summary>
    Task<BackupValidationResult> ValidateBackupAsync(BackupData backupData, CancellationToken cancellationToken = default);
}

public class BackupExportResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public byte[]? Data { get; set; }
    public BackupMetadata? Metadata { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class BackupImportResult
{
    public bool Success { get; set; }
    public int TablesImported { get; set; }
    public int RecordsImported { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class BackupValidationResult
{
    public bool IsValid { get; set; }
    public string? Version { get; set; }
    public DateTime? ExportedAt { get; set; }
    public List<string> Tables { get; set; } = new();
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class BackupMetadata
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; }
    public string AppVersion { get; set; } = "1.0.0";
    public string SourceDatabase { get; set; } = string.Empty;
    public Dictionary<string, int> RecordCounts { get; set; } = new();
}

public class BackupData
{
    public BackupMetadata Metadata { get; set; } = new();
    public BackupSchema Schema { get; set; } = new();
    public Dictionary<string, List<Dictionary<string, object?>>> Data { get; set; } = new();
}

public class BackupSchema
{
    public List<string> Tables { get; set; } = new();
    public List<string> ImportOrder { get; set; } = new();
}
