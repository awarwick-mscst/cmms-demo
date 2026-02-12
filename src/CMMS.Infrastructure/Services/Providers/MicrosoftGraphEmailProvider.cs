using Azure.Identity;
using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace CMMS.Infrastructure.Services.Providers;

public class MicrosoftGraphEmailProvider : IEmailProvider
{
    private readonly EmailCalendarSettings _settings;
    private readonly IIntegrationSettingsService _integrationSettings;
    private readonly ILogger<MicrosoftGraphEmailProvider> _logger;

    public string ProviderName => "MicrosoftGraph";

    public MicrosoftGraphEmailProvider(
        IOptions<EmailCalendarSettings> settings,
        IIntegrationSettingsService integrationSettings,
        ILogger<MicrosoftGraphEmailProvider> logger)
    {
        _settings = settings.Value;
        _integrationSettings = integrationSettings;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(cancellationToken);
            if (graphClient == null)
            {
                return EmailSendResult.Failed("Microsoft Graph client not configured");
            }

            var sharedMailbox = await GetSharedMailboxAsync(cancellationToken);
            if (string.IsNullOrEmpty(sharedMailbox))
            {
                return EmailSendResult.Failed("Shared mailbox not configured");
            }

            var graphMessage = new Message
            {
                Subject = message.Subject,
                Body = new ItemBody
                {
                    ContentType = string.IsNullOrEmpty(message.BodyHtml) ? BodyType.Text : BodyType.Html,
                    Content = message.BodyHtml ?? message.Body
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = message.To }
                    }
                }
            };

            // Add CC recipients
            if (message.Cc.Count > 0)
            {
                graphMessage.CcRecipients = message.Cc.Select(cc => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = cc }
                }).ToList();
            }

            // Add BCC recipients
            if (message.Bcc.Count > 0)
            {
                graphMessage.BccRecipients = message.Bcc.Select(bcc => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = bcc }
                }).ToList();
            }

            var requestBody = new SendMailPostRequestBody
            {
                Message = graphMessage,
                SaveToSentItems = true
            };

            await graphClient.Users[sharedMailbox].SendMail.PostAsync(requestBody, cancellationToken: cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient} via Microsoft Graph", message.To);
            return EmailSendResult.Succeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Microsoft Graph to {Recipient}", message.To);
            return EmailSendResult.Failed(ex.Message);
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

            // Try to get the user to validate the configuration
            var user = await graphClient.Users[sharedMailbox].GetAsync(cancellationToken: cancellationToken);
            return user != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Microsoft Graph email configuration validation failed");
            return false;
        }
    }

    private async Task<GraphServiceClient?> CreateGraphClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            // First try to get settings from database
            var tenantId = await _integrationSettings.GetSettingAsync(ProviderName, "TenantId", cancellationToken);
            var clientId = await _integrationSettings.GetSettingAsync(ProviderName, "ClientId", cancellationToken);
            var clientSecret = await _integrationSettings.GetSettingAsync(ProviderName, "ClientSecret", cancellationToken);

            // Fall back to appsettings if not in database
            tenantId ??= _settings.MicrosoftGraph.TenantId;
            clientId ??= _settings.MicrosoftGraph.ClientId;
            clientSecret ??= _settings.MicrosoftGraph.ClientSecret;

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning("Microsoft Graph credentials not configured");
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
}
