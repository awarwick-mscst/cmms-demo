namespace CMMS.Core.Interfaces;

public interface IEmailProvider
{
    string ProviderName { get; }
    Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string? From { get; set; }
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
}

public class EmailSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }

    public static EmailSendResult Succeeded(string? messageId = null) => new()
    {
        Success = true,
        MessageId = messageId
    };

    public static EmailSendResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
