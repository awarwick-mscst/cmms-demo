namespace CMMS.Shared.DTOs;

public class CreateConversationRequest
{
    public string? Title { get; set; }
}

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ContextType { get; set; }
    public int? AssetId { get; set; }
}

public class RenameConversationRequest
{
    public string Title { get; set; } = string.Empty;
}
