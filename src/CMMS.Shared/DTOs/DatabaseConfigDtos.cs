namespace CMMS.Shared.DTOs;

public class DatabaseSettingsDto
{
    public string Provider { get; set; } = "SqlServer";
    public string Server { get; set; } = "localhost";
    public int? Port { get; set; }
    public string Database { get; set; } = "CMMS";
    public string AuthType { get; set; } = "Windows";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? AdditionalOptions { get; set; }
    public string? FilePath { get; set; }
    public bool IsConfigured { get; set; }
    public string Tier { get; set; } = "Small";
}

public class DatabaseTestRequestDto
{
    public string Provider { get; set; } = "SqlServer";
    public string Server { get; set; } = "localhost";
    public int? Port { get; set; }
    public string Database { get; set; } = "CMMS";
    public string AuthType { get; set; } = "Windows";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? AdditionalOptions { get; set; }
    public string? FilePath { get; set; }
}

public class DatabaseTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ServerVersion { get; set; }
    public string? ErrorDetails { get; set; }
    public int? LatencyMs { get; set; }
}

public class DatabaseProviderInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int DefaultPort { get; set; }
    public bool SupportsWindowsAuth { get; set; }
    public bool RequiresFilePath { get; set; }
    public bool IsSupported { get; set; }
    public string? NotSupportedReason { get; set; }
}
