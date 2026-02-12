using CMMS.Core.Enums;
using CMMS.Core.Interfaces;

namespace CMMS.API.Middleware;

public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseValidationMiddleware> _logger;

    // Paths that bypass license checks
    private static readonly string[] ExcludedPaths = new[]
    {
        "/api/v1/auth",
        "/api/v1/health",
        "/api/v1/license",
        "/swagger",
    };

    public LicenseValidationMiddleware(RequestDelegate next, ILogger<LicenseValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Skip license check for excluded paths
        if (ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Skip for non-API requests (static files, etc.)
        if (!path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        var licenseService = context.RequestServices.GetRequiredService<ILicenseService>();
        var status = licenseService.GetCurrentLicenseStatus();

        switch (status)
        {
            case LicenseStatus.Valid:
                await _next(context);
                return;

            case LicenseStatus.GracePeriod:
                context.Response.Headers.Append("X-License-Warning", "License server unreachable. Operating in grace period.");
                await _next(context);
                return;

            case LicenseStatus.NotActivated:
                context.Response.Headers.Append("X-License-Warning", "No license activated. Please activate a license.");
                await _next(context);
                return;

            case LicenseStatus.Expired:
            case LicenseStatus.Revoked:
                _logger.LogWarning("Request blocked due to {Status} license: {Path}", status, path);
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    error = status == LicenseStatus.Revoked
                        ? "License has been revoked. Please contact support."
                        : "License has expired. Please renew your subscription.",
                    licenseStatus = status.ToString(),
                });
                return;

            default:
                await _next(context);
                return;
        }
    }
}
