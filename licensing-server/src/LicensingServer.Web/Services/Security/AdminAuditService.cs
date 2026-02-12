using LicensingServer.Web.Data;
using LicensingServer.Web.Models;

namespace LicensingServer.Web.Services.Security;

public class AdminAuditService
{
    private readonly LicensingDbContext _db;
    private readonly ILogger<AdminAuditService> _logger;

    public AdminAuditService(LicensingDbContext db, ILogger<AdminAuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(int? userId, string username, bool success, string authMethod,
        string? failureReason, HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var log = new AdminLoginAuditLog
        {
            AdminUserId = userId,
            Username = username,
            Success = success,
            AuthMethod = authMethod,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent.Length > 500 ? userAgent[..500] : userAgent,
            Timestamp = DateTime.UtcNow,
        };

        _db.AdminLoginAuditLogs.Add(log);
        await _db.SaveChangesAsync();

        if (success)
            _logger.LogInformation("Auth success: user={Username}, method={Method}, ip={Ip}", username, authMethod, ipAddress);
        else
            _logger.LogWarning("Auth failure: user={Username}, method={Method}, reason={Reason}, ip={Ip}", username, authMethod, failureReason, ipAddress);
    }
}
