namespace CMMS.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null,
        int? userId = null, string? ipAddress = null, string? userAgent = null);
}
