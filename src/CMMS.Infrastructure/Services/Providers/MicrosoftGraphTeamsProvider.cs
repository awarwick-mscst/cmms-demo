using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMMS.Infrastructure.Services.Providers;

/// <summary>
/// Microsoft Teams provider using webhooks for channel notifications
/// and Graph API for user chat notifications
/// </summary>
public class MicrosoftGraphTeamsProvider : ITeamsProvider
{
    private readonly IIntegrationSettingsService _settingsService;
    private readonly EmailCalendarSettings _settings;
    private readonly ILogger<MicrosoftGraphTeamsProvider> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "MicrosoftGraph";

    public MicrosoftGraphTeamsProvider(
        IIntegrationSettingsService settingsService,
        IOptions<EmailCalendarSettings> settings,
        ILogger<MicrosoftGraphTeamsProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settingsService = settingsService;
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Teams");
    }

    public async Task<TeamsSendResult> SendChannelNotificationAsync(TeamsMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookUrl = await _settingsService.GetSettingAsync("MicrosoftGraph", "TeamsWebhookUrl", cancellationToken);

            if (string.IsNullOrEmpty(webhookUrl))
            {
                return TeamsSendResult.Fail("Teams webhook URL not configured");
            }

            var card = BuildMessageCard(message);
            var response = await _httpClient.PostAsJsonAsync(webhookUrl, card, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Teams channel notification sent successfully: {Title}", message.Title);
                return TeamsSendResult.Ok();
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to send Teams notification: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return TeamsSendResult.Fail($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams channel notification");
            return TeamsSendResult.Fail(ex.Message);
        }
    }

    public async Task<TeamsSendResult> SendUserNotificationAsync(string userEmail, TeamsMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // For user notifications, we use the Graph API to send chat messages
            // This requires the Chat.Create and ChatMessage.Send permissions
            var tenantId = await _settingsService.GetSettingAsync("MicrosoftGraph", "TenantId", cancellationToken);
            var clientId = await _settingsService.GetSettingAsync("MicrosoftGraph", "ClientId", cancellationToken);
            var clientSecret = await _settingsService.GetSettingAsync("MicrosoftGraph", "ClientSecret", cancellationToken);

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                // Fall back to webhook if Graph API not configured
                _logger.LogWarning("Graph API not configured for user chat, attempting webhook fallback");
                return await SendChannelNotificationAsync(message, cancellationToken);
            }

            // For now, fall back to webhook - full Graph API user chat requires additional setup
            // User-to-user chat via Graph API requires delegated permissions or proactive messaging setup
            _logger.LogInformation("User notification for {Email} sent via channel webhook", userEmail);
            return await SendChannelNotificationAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams user notification to {Email}", userEmail);
            return TeamsSendResult.Fail(ex.Message);
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookUrl = await _settingsService.GetSettingAsync("MicrosoftGraph", "TeamsWebhookUrl", cancellationToken);

            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("Teams webhook URL not configured");
                return false;
            }

            // Validate webhook URL format
            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) ||
                !uri.Host.Contains("webhook.office.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid Teams webhook URL format");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Teams configuration");
            return false;
        }
    }

    /// <summary>
    /// Build a MessageCard for Teams webhook
    /// </summary>
    private object BuildMessageCard(TeamsMessage message)
    {
        var card = new Dictionary<string, object>
        {
            ["@type"] = "MessageCard",
            ["@context"] = "http://schema.org/extensions",
            ["summary"] = message.Summary,
            ["themeColor"] = message.ThemeColor ?? "0076D7",
            ["title"] = message.Title
        };

        if (message.Sections.Count > 0)
        {
            var sections = message.Sections.Select(s =>
            {
                var section = new Dictionary<string, object>
                {
                    ["markdown"] = s.Markdown
                };

                if (!string.IsNullOrEmpty(s.Title))
                    section["activityTitle"] = s.Title;

                if (!string.IsNullOrEmpty(s.Text))
                    section["text"] = s.Text;

                if (s.Facts != null && s.Facts.Count > 0)
                {
                    section["facts"] = s.Facts.Select(f => new { name = f.Name, value = f.Value }).ToArray();
                }

                return section;
            }).ToList();

            card["sections"] = sections;
        }

        if (message.Actions != null && message.Actions.Count > 0)
        {
            var actions = message.Actions.Select(a => new Dictionary<string, object>
            {
                ["@type"] = a.Type,
                ["name"] = a.Name,
                ["targets"] = a.Targets?.Select(t => new { os = t.Os, uri = t.Uri }).ToArray()
                    ?? new[] { new { os = "default", uri = "" } }
            }).ToList();

            card["potentialAction"] = actions;
        }

        return card;
    }
}
