namespace CMMS.Core.Entities;

public class AiConversation : BaseEntity
{
    public string Title { get; set; } = "New Conversation";
    public int UserId { get; set; }
    public string? Summary { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}
