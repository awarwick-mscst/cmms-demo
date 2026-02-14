namespace CMMS.Core.Interfaces;

public interface IAiAssistantService
{
    // Conversation management
    Task<AiConversationDto> CreateConversationAsync(int userId, string? title = null, CancellationToken ct = default);
    Task<IEnumerable<AiConversationDto>> GetUserConversationsAsync(int userId, CancellationToken ct = default);
    Task<AiConversationDetailDto?> GetConversationAsync(int conversationId, int userId, CancellationToken ct = default);
    Task<bool> DeleteConversationAsync(int conversationId, int userId, CancellationToken ct = default);
    Task<bool> RenameConversationAsync(int conversationId, int userId, string title, CancellationToken ct = default);

    // Messaging
    Task<AiMessageDto> SendMessageAsync(int conversationId, int userId, string message, string? contextType = null, int? assetId = null, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamMessageAsync(int conversationId, int userId, string message, string? contextType = null, int? assetId = null, CancellationToken ct = default);

    // Status
    Task<AiStatusDto> GetStatusAsync(CancellationToken ct = default);
}

// DTOs defined here alongside the interface for simplicity
public record AiConversationDto(int Id, string Title, string? Summary, DateTime CreatedAt, DateTime? UpdatedAt);
public record AiConversationDetailDto(int Id, string Title, string? Summary, DateTime CreatedAt, DateTime? UpdatedAt, List<AiMessageDto> Messages);
public record AiMessageDto(int Id, string Role, string Content, string? ContextType, DateTime CreatedAt);
public record AiStatusDto(bool Enabled, bool Reachable, string? Model);
