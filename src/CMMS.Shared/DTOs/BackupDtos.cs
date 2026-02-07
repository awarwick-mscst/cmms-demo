namespace CMMS.Shared.DTOs;

public class BackupInfoDto
{
    public List<string> Tables { get; set; } = new();
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public int TotalRecords { get; set; }
    public long EstimatedSizeBytes { get; set; }
}

public class BackupValidationDto
{
    public bool IsValid { get; set; }
    public string? Version { get; set; }
    public DateTime? ExportedAt { get; set; }
    public List<string> Tables { get; set; } = new();
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public int TotalRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class BackupImportResultDto
{
    public bool Success { get; set; }
    public int TablesImported { get; set; }
    public int RecordsImported { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
