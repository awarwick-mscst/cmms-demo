using CMMS.API.Attributes;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/admin/integrations")]
[Authorize(Roles = "Admin")]
[RequiresFeature("email-calendar")]
public class IntegrationAdminController : ControllerBase
{
    private readonly IIntegrationSettingsService _settingsService;
    private readonly IEmailProvider _emailProvider;
    private readonly ICalendarProvider _calendarProvider;
    private readonly ITeamsProvider _teamsProvider;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IntegrationAdminController> _logger;

    public IntegrationAdminController(
        IIntegrationSettingsService settingsService,
        IEmailProvider emailProvider,
        ICalendarProvider calendarProvider,
        ITeamsProvider teamsProvider,
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<IntegrationAdminController> logger)
    {
        _settingsService = settingsService;
        _emailProvider = emailProvider;
        _calendarProvider = calendarProvider;
        _teamsProvider = teamsProvider;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<IntegrationSettingsDto>>>> GetIntegrations(
        CancellationToken cancellationToken = default)
    {
        var providers = new[] { "MicrosoftGraph", "Gmail" };
        var integrations = new List<IntegrationSettingsDto>();

        foreach (var provider in providers)
        {
            var isConfigured = await _settingsService.IsProviderConfiguredAsync(provider, cancellationToken);
            var isValid = false;

            if (isConfigured && provider == "MicrosoftGraph")
            {
                isValid = await _emailProvider.ValidateConfigurationAsync(cancellationToken);
            }

            integrations.Add(new IntegrationSettingsDto
            {
                ProviderType = provider,
                IsConfigured = isConfigured,
                IsValid = isValid
            });
        }

        return Ok(ApiResponse<IEnumerable<IntegrationSettingsDto>>.Ok(integrations));
    }

    [HttpGet("microsoft-graph")]
    public async Task<ActionResult<ApiResponse<MicrosoftGraphSettingsDto>>> GetMicrosoftGraphSettings(
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetProviderSettingsAsync("MicrosoftGraph", cancellationToken);

        var dto = new MicrosoftGraphSettingsDto
        {
            TenantId = settings.GetValueOrDefault("TenantId", string.Empty),
            ClientId = settings.GetValueOrDefault("ClientId", string.Empty),
            // Don't return actual secret, just indicate if it's set
            ClientSecret = settings.ContainsKey("ClientSecret") ? "********" : string.Empty,
            SharedMailbox = settings.GetValueOrDefault("SharedMailbox", string.Empty),
            SharedCalendarId = settings.GetValueOrDefault("SharedCalendarId", string.Empty),
            TeamsWebhookUrl = settings.GetValueOrDefault("TeamsWebhookUrl", string.Empty)
        };

        return Ok(ApiResponse<MicrosoftGraphSettingsDto>.Ok(dto));
    }

    [HttpPut("microsoft-graph")]
    public async Task<ActionResult<ApiResponse>> UpdateMicrosoftGraphSettings(
        [FromBody] UpdateMicrosoftGraphSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = new Dictionary<string, string>
        {
            ["TenantId"] = request.TenantId,
            ["ClientId"] = request.ClientId,
            ["SharedMailbox"] = request.SharedMailbox
        };

        // Only update secret if it's not the placeholder
        if (!string.IsNullOrEmpty(request.ClientSecret) && request.ClientSecret != "********")
        {
            settings["ClientSecret"] = request.ClientSecret;
        }

        if (!string.IsNullOrEmpty(request.SharedCalendarId))
        {
            settings["SharedCalendarId"] = request.SharedCalendarId;
        }

        if (!string.IsNullOrEmpty(request.TeamsWebhookUrl))
        {
            settings["TeamsWebhookUrl"] = request.TeamsWebhookUrl;
        }

        await _settingsService.SetProviderSettingsAsync("MicrosoftGraph", settings, cancellationToken);

        _logger.LogInformation("Microsoft Graph settings updated by admin");
        return Ok(ApiResponse.Ok("Microsoft Graph settings updated successfully"));
    }

    [HttpPost("microsoft-graph/validate")]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateMicrosoftGraphSettings(
        CancellationToken cancellationToken = default)
    {
        var emailValid = await _emailProvider.ValidateConfigurationAsync(cancellationToken);
        var calendarValid = await _calendarProvider.ValidateConfigurationAsync(cancellationToken);

        var isValid = emailValid && calendarValid;
        var message = isValid
            ? "Configuration is valid"
            : $"Validation failed - Email: {(emailValid ? "OK" : "Failed")}, Calendar: {(calendarValid ? "OK" : "Failed")}";

        return Ok(ApiResponse<bool>.Ok(isValid, message));
    }

    [HttpPost("test-email")]
    public async Task<ActionResult<ApiResponse>> SendTestEmail(
        [FromBody] TestEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
            return BadRequest(ApiResponse.Fail("Email address is required"));

        var message = new EmailMessage
        {
            To = request.ToEmail,
            Subject = "CMMS Test Email",
            Body = "This is a test email from the CMMS notification system. If you received this, the email configuration is working correctly.",
            BodyHtml = @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #1976d2; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>CMMS Test Email</h2>
        </div>
        <div class='content'>
            <p>This is a test email from the CMMS notification system.</p>
            <p>If you received this, the email configuration is working correctly.</p>
        </div>
    </div>
</body>
</html>"
        };

        var result = await _emailProvider.SendEmailAsync(message, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Test email sent to {Email}", request.ToEmail);
            return Ok(ApiResponse.Ok($"Test email sent successfully to {request.ToEmail}"));
        }

        return BadRequest(ApiResponse.Fail($"Failed to send test email: {result.ErrorMessage}"));
    }

    [HttpPost("test-calendar")]
    public async Task<ActionResult<ApiResponse<string>>> CreateTestCalendarEvent(
        [FromBody] TestCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = request.StartTime ?? DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(request.DurationMinutes);

        var calendarRequest = new CalendarEventRequest
        {
            Title = request.Title,
            Description = "This is a test calendar event from CMMS. You can delete this event.",
            StartTime = startTime,
            EndTime = endTime
        };

        var result = await _calendarProvider.CreateSharedEventAsync(calendarRequest, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Test calendar event created: {EventId}", result.EventId);
            return Ok(ApiResponse<string>.Ok(result.EventId!, "Test calendar event created successfully"));
        }

        return BadRequest(ApiResponse<string>.Fail($"Failed to create test calendar event: {result.ErrorMessage}"));
    }

    [HttpPost("test-teams")]
    public async Task<ActionResult<ApiResponse>> SendTestTeamsNotification(
        CancellationToken cancellationToken = default)
    {
        var message = new TeamsMessage
        {
            Title = "CMMS Test Notification",
            Summary = "Test notification from CMMS",
            ThemeColor = "0076D7",
            Sections = new List<TeamsMessageSection>
            {
                new TeamsMessageSection
                {
                    Title = "Test Notification",
                    Text = "This is a test notification from the CMMS system. If you see this message, the Teams integration is working correctly.",
                    Facts = new List<TeamsMessageFact>
                    {
                        new TeamsMessageFact { Name = "Sent At", Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                        new TeamsMessageFact { Name = "Status", Value = "Test Message" }
                    }
                }
            }
        };

        var result = await _teamsProvider.SendChannelNotificationAsync(message, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Test Teams notification sent successfully");
            return Ok(ApiResponse.Ok("Test Teams notification sent successfully"));
        }

        return BadRequest(ApiResponse.Fail($"Failed to send Teams notification: {result.ErrorMessage}"));
    }

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationQueueDto>>>> GetNotificationQueue(
        [FromQuery] NotificationQueueFilter filter,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _unitOfWork.NotificationQueue.Query()
            .Include(n => n.RecipientUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<NotificationStatus>(filter.Status, out var status))
        {
            baseQuery = baseQuery.Where(n => n.Status == status);
        }

        if (!string.IsNullOrEmpty(filter.Type) && Enum.TryParse<NotificationType>(filter.Type, out var type))
        {
            baseQuery = baseQuery.Where(n => n.Type == type);
        }

        if (!string.IsNullOrEmpty(filter.ReferenceType))
        {
            baseQuery = baseQuery.Where(n => n.ReferenceType == filter.ReferenceType);
        }

        if (filter.ReferenceId.HasValue)
        {
            baseQuery = baseQuery.Where(n => n.ReferenceId == filter.ReferenceId);
        }

        if (filter.From.HasValue)
        {
            baseQuery = baseQuery.Where(n => n.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            baseQuery = baseQuery.Where(n => n.CreatedAt <= filter.To.Value);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .OrderByDescending(n => n.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(n => new NotificationQueueDto
        {
            Id = n.Id,
            Type = n.Type.ToString(),
            RecipientUserId = n.RecipientUserId,
            RecipientEmail = n.RecipientEmail,
            RecipientName = n.RecipientUser != null ? $"{n.RecipientUser.FirstName} {n.RecipientUser.LastName}" : null,
            Subject = n.Subject,
            Status = n.Status.ToString(),
            RetryCount = n.RetryCount,
            ScheduledFor = n.ScheduledFor,
            ProcessedAt = n.ProcessedAt,
            ErrorMessage = n.ErrorMessage,
            ReferenceType = n.ReferenceType,
            ReferenceId = n.ReferenceId,
            CreatedAt = n.CreatedAt
        }).ToList();

        var result = new PagedResult<NotificationQueueDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return Ok(ApiResponse<PagedResult<NotificationQueueDto>>.Ok(result));
    }

    [HttpGet("logs")]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationLogDto>>>> GetNotificationLogs(
        [FromQuery] NotificationLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.NotificationLogs.Query();

        if (filter.Success.HasValue)
        {
            query = query.Where(l => l.Success == filter.Success.Value);
        }

        if (!string.IsNullOrEmpty(filter.Type) && Enum.TryParse<NotificationType>(filter.Type, out var type))
        {
            query = query.Where(l => l.Type == type);
        }

        if (!string.IsNullOrEmpty(filter.Channel) && Enum.TryParse<NotificationChannel>(filter.Channel, out var channel))
        {
            query = query.Where(l => l.Channel == channel);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(l => l.SentAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(l => l.SentAt <= filter.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.SentAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(l => new NotificationLogDto
        {
            Id = l.Id,
            Type = l.Type.ToString(),
            RecipientEmail = l.RecipientEmail,
            Subject = l.Subject,
            Channel = l.Channel.ToString(),
            Success = l.Success,
            ExternalMessageId = l.ExternalMessageId,
            ErrorMessage = l.ErrorMessage,
            SentAt = l.SentAt,
            ReferenceType = l.ReferenceType,
            ReferenceId = l.ReferenceId
        }).ToList();

        var result = new PagedResult<NotificationLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return Ok(ApiResponse<PagedResult<NotificationLogDto>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<NotificationStatsDto>>> GetNotificationStats(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var pendingCount = await _unitOfWork.NotificationQueue.Query()
            .CountAsync(n => n.Status == NotificationStatus.Pending, cancellationToken);

        var processingCount = await _unitOfWork.NotificationQueue.Query()
            .CountAsync(n => n.Status == NotificationStatus.Processing, cancellationToken);

        var sentToday = await _unitOfWork.NotificationLogs.Query()
            .CountAsync(l => l.Success && l.SentAt.Date == today, cancellationToken);

        var failedToday = await _unitOfWork.NotificationLogs.Query()
            .CountAsync(l => !l.Success && l.SentAt.Date == today, cancellationToken);

        var totalSent = await _unitOfWork.NotificationLogs.Query()
            .CountAsync(l => l.Success, cancellationToken);

        var totalFailed = await _unitOfWork.NotificationLogs.Query()
            .CountAsync(l => !l.Success, cancellationToken);

        var stats = new NotificationStatsDto
        {
            PendingCount = pendingCount,
            ProcessingCount = processingCount,
            SentToday = sentToday,
            FailedToday = failedToday,
            TotalSent = totalSent,
            TotalFailed = totalFailed
        };

        return Ok(ApiResponse<NotificationStatsDto>.Ok(stats));
    }

    [HttpPost("queue/{id}/retry")]
    public async Task<ActionResult<ApiResponse>> RetryNotification(int id, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.NotificationQueue.GetByIdAsync(id, cancellationToken);
        if (notification == null)
            return NotFound(ApiResponse.Fail("Notification not found"));

        notification.Status = NotificationStatus.Pending;
        notification.RetryCount = 0;
        notification.ErrorMessage = null;
        notification.ScheduledFor = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification {Id} manually queued for retry", id);
        return Ok(ApiResponse.Ok("Notification queued for retry"));
    }

    [HttpDelete("queue/{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteNotification(int id, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.NotificationQueue.GetByIdAsync(id, cancellationToken);
        if (notification == null)
            return NotFound(ApiResponse.Fail("Notification not found"));

        notification.IsDeleted = true;
        notification.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification {Id} deleted", id);
        return Ok(ApiResponse.Ok("Notification deleted"));
    }
}
