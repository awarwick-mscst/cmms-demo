namespace CMMS.Core.Interfaces;

/// <summary>
/// Interface for sending Microsoft Teams notifications
/// </summary>
public interface ITeamsProvider
{
    /// <summary>
    /// Provider name identifier
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Send a notification to a Teams channel via webhook
    /// </summary>
    Task<TeamsSendResult> SendChannelNotificationAsync(TeamsMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a notification to a user via Teams chat
    /// </summary>
    Task<TeamsSendResult> SendUserNotificationAsync(string userEmail, TeamsMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate the Teams configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Teams notification message
/// </summary>
public class TeamsMessage
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ThemeColor { get; set; }
    public List<TeamsMessageSection> Sections { get; set; } = new();
    public List<TeamsMessageAction>? Actions { get; set; }
}

/// <summary>
/// Section within a Teams message card
/// </summary>
public class TeamsMessageSection
{
    public string? Title { get; set; }
    public string? Text { get; set; }
    public List<TeamsMessageFact>? Facts { get; set; }
    public bool Markdown { get; set; } = true;
}

/// <summary>
/// Fact (key-value pair) within a Teams message section
/// </summary>
public class TeamsMessageFact
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Action button in a Teams message
/// </summary>
public class TeamsMessageAction
{
    public string Type { get; set; } = "OpenUri";
    public string Name { get; set; } = string.Empty;
    public List<TeamsActionTarget>? Targets { get; set; }
}

/// <summary>
/// Target for a Teams message action
/// </summary>
public class TeamsActionTarget
{
    public string Os { get; set; } = "default";
    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// Result of sending a Teams notification
/// </summary>
public class TeamsSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }

    public static TeamsSendResult Ok(string? messageId = null) => new() { Success = true, MessageId = messageId };
    public static TeamsSendResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
