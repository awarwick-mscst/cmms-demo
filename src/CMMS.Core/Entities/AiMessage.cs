namespace CMMS.Core.Entities;

public class AiMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } = "user"; // user, assistant, system
    public string Content { get; set; } = string.Empty;
    public string? ContextType { get; set; } // predictive, downtime_followup, overdue, asset_health
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual AiConversation Conversation { get; set; } = null!;
}
