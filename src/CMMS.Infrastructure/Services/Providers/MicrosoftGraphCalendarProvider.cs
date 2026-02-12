using Azure.Identity;
using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace CMMS.Infrastructure.Services.Providers;

public class MicrosoftGraphCalendarProvider : ICalendarProvider
{
    private readonly EmailCalendarSettings _settings;
    private readonly IIntegrationSettingsService _integrationSettings;
    private readonly ILogger<MicrosoftGraphCalendarProvider> _logger;

    public string ProviderName => "MicrosoftGraph";

    public MicrosoftGraphCalendarProvider(
        IOptions<EmailCalendarSettings> settings,
        IIntegrationSettingsService integrationSettings,
        ILogger<MicrosoftGraphCalendarProvider> logger)
    {
        _settings = settings.Value;
        _integrationSettings = integrationSettings;
        _logger = logger;
    }

    public async Task<CalendarEventResult> CreateSharedEventAsync(CalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
            {
                return CalendarEventResult.Failed("Microsoft Graph client not configured");
            }

            var sharedMailbox = await GetSharedMailboxAsync(cancellationToken);
            var calendarId = await GetSharedCalendarIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(sharedMailbox))
            {
                return CalendarEventResult.Failed("Shared mailbox not configured");
            }

            var graphEvent = CreateGraphEvent(request);

            Event? createdEvent;
            if (!string.IsNullOrEmpty(calendarId))
            {
                createdEvent = await graphClient.Users[sharedMailbox]
                    .Calendars[calendarId]
                    .Events
                    .PostAsync(graphEvent, cancellationToken: cancellationToken);
            }
            else
            {
                createdEvent = await graphClient.Users[sharedMailbox]
                    .Calendar
                    .Events
                    .PostAsync(graphEvent, cancellationToken: cancellationToken);
            }

            if (createdEvent?.Id == null)
            {
                return CalendarEventResult.Failed("Failed to create calendar event");
            }

            _logger.LogInformation("Created shared calendar event: {EventId} for {Title}", createdEvent.Id, request.Title);
            return CalendarEventResult.Succeeded(createdEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shared calendar event: {Title}", request.Title);
            return CalendarEventResult.Failed(ex.Message);
        }
    }

    public async Task<CalendarEventResult> CreateUserEventAsync(string userEmail, CalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
            {
                return CalendarEventResult.Failed("Microsoft Graph client not configured");
            }

            var graphEvent = CreateGraphEvent(request);

            var createdEvent = await graphClient.Users[userEmail]
                .Calendar
                .Events
                .PostAsync(graphEvent, cancellationToken: cancellationToken);

            if (createdEvent?.Id == null)
            {
                return CalendarEventResult.Failed("Failed to create user calendar event");
            }

            _logger.LogInformation("Created user calendar event: {EventId} for {User} - {Title}", createdEvent.Id, userEmail, request.Title);
            return CalendarEventResult.Succeeded(createdEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user calendar event for {User}: {Title}", userEmail, request.Title);
            return CalendarEventResult.Failed(ex.Message);
        }
    }

    public async Task<CalendarEventResult> UpdateSharedEventAsync(string eventId, CalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
            {
                return CalendarEventResult.Failed("Microsoft Graph client not configured");
            }

            var sharedMailbox = await GetSharedMailboxAsync(cancellationToken);
            var calendarId = await GetSharedCalendarIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(sharedMailbox))
            {
                return CalendarEventResult.Failed("Shared mailbox not configured");
            }

            var graphEvent = CreateGraphEvent(request);

            Event? updatedEvent;
            if (!string.IsNullOrEmpty(calendarId))
            {
                updatedEvent = await graphClient.Users[sharedMailbox]
                    .Calendars[calendarId]
                    .Events[eventId]
                    .PatchAsync(graphEvent, cancellationToken: cancellationToken);
            }
            else
            {
                updatedEvent = await graphClient.Users[sharedMailbox]
                    .Calendar
                    .Events[eventId]
                    .PatchAsync(graphEvent, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Updated shared calendar event: {EventId}", eventId);
            return CalendarEventResult.Succeeded(updatedEvent?.Id ?? eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update shared calendar event: {EventId}", eventId);
            return CalendarEventResult.Failed(ex.Message);
        }
    }

    public async Task<CalendarEventResult> UpdateUserEventAsync(string userEmail, string eventId, CalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
            {
                return CalendarEventResult.Failed("Microsoft Graph client not configured");
            }

            var graphEvent = CreateGraphEvent(request);

            var updatedEvent = await graphClient.Users[userEmail]
                .Calendar
                .Events[eventId]
                .PatchAsync(graphEvent, cancellationToken: cancellationToken);

            _logger.LogInformation("Updated user calendar event: {EventId} for {User}", eventId, userEmail);
            return CalendarEventResult.Succeeded(updatedEvent?.Id ?? eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user calendar event: {EventId} for {User}", eventId, userEmail);
            return CalendarEventResult.Failed(ex.Message);
        }
    }

    public async Task<bool> DeleteSharedEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
                return false;

            var sharedMailbox = await GetSharedMailboxAsync(cancellationToken);
            var calendarId = await GetSharedCalendarIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(sharedMailbox))
                return false;

            if (!string.IsNullOrEmpty(calendarId))
            {
                await graphClient.Users[sharedMailbox]
                    .Calendars[calendarId]
                    .Events[eventId]
                    .DeleteAsync(cancellationToken: cancellationToken);
            }
            else
            {
                await graphClient.Users[sharedMailbox]
                    .Calendar
                    .Events[eventId]
                    .DeleteAsync(cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Deleted shared calendar event: {EventId}", eventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete shared calendar event: {EventId}", eventId);
            return false;
        }
    }

    public async Task<bool> DeleteUserEventAsync(string userEmail, string eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
                return false;

            await graphClient.Users[userEmail]
                .Calendar
                .Events[eventId]
                .DeleteAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted user calendar event: {EventId} for {User}", eventId, userEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user calendar event: {EventId} for {User}", eventId, userEmail);
            return false;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
                return false;

            var sharedMailbox = await GetSharedMailboxAsync(cancellationToken);
            if (string.IsNullOrEmpty(sharedMailbox))
                return false;

            // Try to access the calendar to validate
            var calendar = await graphClient.Users[sharedMailbox].Calendar.GetAsync(cancellationToken: cancellationToken);
            return calendar != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Microsoft Graph calendar configuration validation failed");
            return false;
        }
    }

    private Event CreateGraphEvent(CalendarEventRequest request)
    {
        var graphEvent = new Event
        {
            Subject = request.Title,
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = request.Description ?? string.Empty
            },
            Start = new DateTimeTimeZone
            {
                DateTime = request.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "UTC"
            },
            End = new DateTimeTimeZone
            {
                DateTime = request.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "UTC"
            },
            IsAllDay = request.IsAllDay
        };

        if (!string.IsNullOrEmpty(request.Location))
        {
            graphEvent.Location = new Location { DisplayName = request.Location };
        }

        if (request.Attendees.Count > 0)
        {
            graphEvent.Attendees = request.Attendees.Select(a => new Attendee
            {
                EmailAddress = new EmailAddress { Address = a },
                Type = AttendeeType.Required
            }).ToList();
        }

        return graphEvent;
    }

    private async Task<GraphServiceClient?> CreateGraphClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = await _integrationSettings.GetSettingAsync(ProviderName, "TenantId", cancellationToken);
            var clientId = await _integrationSettings.GetSettingAsync(ProviderName, "ClientId", cancellationToken);
            var clientSecret = await _integrationSettings.GetSettingAsync(ProviderName, "ClientSecret", cancellationToken);

            tenantId ??= _settings.MicrosoftGraph.TenantId;
            clientId ??= _settings.MicrosoftGraph.ClientId;
            clientSecret ??= _settings.MicrosoftGraph.ClientSecret;

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return null;
            }

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            return new GraphServiceClient(credential, scopes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Microsoft Graph client");
            return null;
        }
    }

    private async Task<string?> GetSharedMailboxAsync(CancellationToken cancellationToken)
    {
        var mailbox = await _integrationSettings.GetSettingAsync(ProviderName, "SharedMailbox", cancellationToken);
        return mailbox ?? _settings.MicrosoftGraph.SharedMailbox;
    }

    private async Task<string?> GetSharedCalendarIdAsync(CancellationToken cancellationToken)
    {
        var calendarId = await _integrationSettings.GetSettingAsync(ProviderName, "SharedCalendarId", cancellationToken);
        return calendarId ?? _settings.MicrosoftGraph.SharedCalendarId;
    }
}
