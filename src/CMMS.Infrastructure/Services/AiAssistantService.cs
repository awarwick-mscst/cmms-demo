using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CMMS.Core.Configuration;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMMS.Infrastructure.Services;

public class AiAssistantService : IAiAssistantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiAssistantSettings _settings;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        IOptions<AiAssistantSettings> settings,
        ILogger<AiAssistantService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AiConversationDto> CreateConversationAsync(int userId, string? title = null, CancellationToken ct = default)
    {
        var conversation = new AiConversation
        {
            Title = title ?? "New Conversation",
            UserId = userId,
            CreatedBy = userId
        };

        await _unitOfWork.AiConversations.AddAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(conversation);
    }

    public async Task<IEnumerable<AiConversationDto>> GetUserConversationsAsync(int userId, CancellationToken ct = default)
    {
        var conversations = await _unitOfWork.AiConversations.Query()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Select(c => new AiConversationDto(c.Id, c.Title, c.Summary, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(ct);

        return conversations;
    }

    public async Task<AiConversationDetailDto?> GetConversationAsync(int conversationId, int userId, CancellationToken ct = default)
    {
        var conversation = await _unitOfWork.AiConversations.Query()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (conversation == null) return null;

        return new AiConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.Summary,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Messages.Select(m => new AiMessageDto(m.Id, m.Role, m.Content, m.ContextType, m.CreatedAt)).ToList()
        );
    }

    public async Task<bool> DeleteConversationAsync(int conversationId, int userId, CancellationToken ct = default)
    {
        var conversation = await _unitOfWork.AiConversations.FirstOrDefaultAsync(
            c => c.Id == conversationId && c.UserId == userId, ct);
        if (conversation == null) return false;

        conversation.IsDeleted = true;
        conversation.DeletedAt = DateTime.UtcNow;
        _unitOfWork.AiConversations.Update(conversation);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RenameConversationAsync(int conversationId, int userId, string title, CancellationToken ct = default)
    {
        var conversation = await _unitOfWork.AiConversations.FirstOrDefaultAsync(
            c => c.Id == conversationId && c.UserId == userId, ct);
        if (conversation == null) return false;

        conversation.Title = title;
        conversation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.AiConversations.Update(conversation);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AiMessageDto> SendMessageAsync(int conversationId, int userId, string message, string? contextType = null, int? assetId = null, CancellationToken ct = default)
    {
        var conversation = await _unitOfWork.AiConversations.Query()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (conversation == null)
            throw new InvalidOperationException("Conversation not found");

        // Save user message
        var userMessage = new AiMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = message,
            ContextType = contextType
        };
        await _unitOfWork.AiMessages.AddAsync(userMessage, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Build context
        var contextData = await BuildContextAsync(contextType, assetId, ct);

        // Build messages for API
        var apiMessages = BuildApiMessages(conversation.Messages.ToList(), userMessage, contextData);

        // Call LLM
        var assistantContent = await CallLlmAsync(apiMessages, ct);

        // Save assistant message
        var assistantMessage = new AiMessage
        {
            ConversationId = conversationId,
            Role = "assistant",
            Content = assistantContent,
            ContextType = contextType
        };
        await _unitOfWork.AiMessages.AddAsync(assistantMessage, ct);

        // Update conversation timestamp
        conversation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.AiConversations.Update(conversation);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AiMessageDto(assistantMessage.Id, assistantMessage.Role, assistantMessage.Content, assistantMessage.ContextType, assistantMessage.CreatedAt);
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(int conversationId, int userId, string message, string? contextType = null, int? assetId = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var conversation = await _unitOfWork.AiConversations.Query()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (conversation == null)
            throw new InvalidOperationException("Conversation not found");

        // Save user message
        var userMessage = new AiMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = message,
            ContextType = contextType
        };
        await _unitOfWork.AiMessages.AddAsync(userMessage, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Build context
        var contextData = await BuildContextAsync(contextType, assetId, ct);

        // Build messages for API
        var apiMessages = BuildApiMessages(conversation.Messages.ToList(), userMessage, contextData);

        // Stream from LLM
        var fullResponse = new StringBuilder();
        await foreach (var chunk in StreamLlmAsync(apiMessages, ct))
        {
            fullResponse.Append(chunk);
            yield return chunk;
        }

        // Save assistant message after stream completes
        var assistantMessage = new AiMessage
        {
            ConversationId = conversationId,
            Role = "assistant",
            Content = fullResponse.ToString(),
            ContextType = contextType
        };
        await _unitOfWork.AiMessages.AddAsync(assistantMessage, ct);

        conversation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.AiConversations.Update(conversation);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<AiStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        if (!_settings.Enabled)
            return new AiStatusDto(false, false, null);

        try
        {
            var client = _httpClientFactory.CreateClient("AiAssistant");
            var response = await client.GetAsync("models", ct);
            return new AiStatusDto(true, response.IsSuccessStatusCode, _settings.Model);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI assistant health check failed");
            return new AiStatusDto(true, false, _settings.Model);
        }
    }

    // --- Context Builders ---

    private async Task<string?> BuildContextAsync(string? contextType, int? assetId, CancellationToken ct)
    {
        return contextType switch
        {
            "predictive" => await BuildPredictiveMaintenanceContextAsync(assetId, ct),
            "downtime_followup" => await BuildDowntimeFollowUpContextAsync(ct),
            "overdue" => await BuildOverdueMaintenanceContextAsync(ct),
            "asset_health" => assetId.HasValue ? await BuildAssetHealthContextAsync(assetId.Value, ct) : null,
            _ => null
        };
    }

    private async Task<string> BuildPredictiveMaintenanceContextAsync(int? assetId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CMMS DATA CONTEXT: PREDICTIVE MAINTENANCE ANALYSIS ===\n");

        // Recent repair work orders (90 days)
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var repairWoQuery = _unitOfWork.WorkOrders.Query()
            .Include(w => w.Asset)
            .Include(w => w.AssignedTo)
            .Where(w => w.Type == WorkOrderType.Repair && w.CreatedAt >= cutoff);

        if (assetId.HasValue)
            repairWoQuery = repairWoQuery.Where(w => w.AssetId == assetId.Value);

        var recentRepairs = await repairWoQuery
            .OrderByDescending(w => w.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        sb.AppendLine($"## Recent Repair Work Orders (last 90 days): {recentRepairs.Count}");
        foreach (var wo in recentRepairs)
        {
            sb.AppendLine($"- [{wo.WorkOrderNumber}] {wo.Title} | Asset: {wo.Asset?.Name ?? "N/A"} | Priority: {wo.Priority} | Status: {wo.Status} | Created: {wo.CreatedAt:yyyy-MM-dd}");
        }

        // Overdue PM schedules
        var overduePms = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(pm => pm.Asset)
            .Where(pm => pm.IsActive && pm.NextDueDate != null && pm.NextDueDate < DateTime.UtcNow)
            .ToListAsync(ct);

        sb.AppendLine($"\n## Overdue PM Schedules: {overduePms.Count}");
        foreach (var pm in overduePms)
        {
            sb.AppendLine($"- {pm.Name} | Asset: {pm.Asset?.Name ?? "N/A"} | Due: {pm.NextDueDate:yyyy-MM-dd} | Frequency: {pm.FrequencyType}/{pm.FrequencyValue}");
        }

        // Critical/High criticality assets
        var criticalAssets = await _unitOfWork.Assets.Query()
            .Where(a => a.Criticality == AssetCriticality.Critical || a.Criticality == AssetCriticality.High)
            .Select(a => new { a.Name, a.Criticality, a.Status, a.LastMaintenanceDate, a.NextMaintenanceDate })
            .ToListAsync(ct);

        sb.AppendLine($"\n## Critical/High Criticality Assets: {criticalAssets.Count}");
        foreach (var asset in criticalAssets)
        {
            sb.AppendLine($"- {asset.Name} | Criticality: {asset.Criticality} | Status: {asset.Status} | Last Maint: {asset.LastMaintenanceDate?.ToString("yyyy-MM-dd") ?? "Never"} | Next: {asset.NextMaintenanceDate?.ToString("yyyy-MM-dd") ?? "Not scheduled"}");
        }

        return sb.ToString();
    }

    private async Task<string> BuildDowntimeFollowUpContextAsync(CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CMMS DATA CONTEXT: DOWN MACHINES FOLLOW-UP ===\n");

        // Assets currently in maintenance
        var downAssets = await _unitOfWork.Assets.Query()
            .Include(a => a.Location)
            .Include(a => a.Category)
            .Where(a => a.Status == AssetStatus.InMaintenance)
            .ToListAsync(ct);

        sb.AppendLine($"## Assets Currently Down (InMaintenance): {downAssets.Count}");

        foreach (var asset in downAssets)
        {
            sb.AppendLine($"\n### {asset.Name} (Tag: {asset.AssetTag})");
            sb.AppendLine($"   Location: {asset.Location?.Name ?? "N/A"} | Category: {asset.Category?.Name ?? "N/A"} | Criticality: {asset.Criticality}");

            // Find open work orders for this asset
            var openWos = await _unitOfWork.WorkOrders.Query()
                .Include(w => w.AssignedTo)
                .Include(w => w.LaborEntries)
                .Where(w => w.AssetId == asset.Id && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled)
                .ToListAsync(ct);

            foreach (var wo in openWos)
            {
                var totalHours = wo.LaborEntries.Sum(l => l.HoursWorked);
                sb.AppendLine($"   WO: [{wo.WorkOrderNumber}] {wo.Title} | Status: {wo.Status} | Priority: {wo.Priority} | Assigned: {wo.AssignedTo?.FirstName ?? "Unassigned"} | Labor: {totalHours:F1}h");
            }

            if (!openWos.Any())
                sb.AppendLine("   WARNING: No open work orders for this down asset!");
        }

        return sb.ToString();
    }

    private async Task<string> BuildOverdueMaintenanceContextAsync(CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CMMS DATA CONTEXT: OVERDUE MAINTENANCE ===\n");

        // Overdue PM schedules
        var overduePms = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(pm => pm.Asset)
            .Where(pm => pm.IsActive && pm.NextDueDate != null && pm.NextDueDate < DateTime.UtcNow)
            .OrderBy(pm => pm.NextDueDate)
            .ToListAsync(ct);

        sb.AppendLine($"## Overdue PM Schedules: {overduePms.Count}");
        foreach (var pm in overduePms)
        {
            var daysOverdue = (DateTime.UtcNow - pm.NextDueDate!.Value).Days;
            sb.AppendLine($"- {pm.Name} | Asset: {pm.Asset?.Name ?? "N/A"} | Due: {pm.NextDueDate:yyyy-MM-dd} | {daysOverdue} days overdue | Priority: {pm.Priority}");
        }

        // Overdue work orders (past scheduled end date, not completed)
        var overdueWos = await _unitOfWork.WorkOrders.Query()
            .Include(w => w.Asset)
            .Include(w => w.AssignedTo)
            .Where(w => w.ScheduledEndDate != null && w.ScheduledEndDate < DateTime.UtcNow
                        && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled)
            .OrderBy(w => w.ScheduledEndDate)
            .Take(50)
            .ToListAsync(ct);

        sb.AppendLine($"\n## Overdue Work Orders: {overdueWos.Count}");
        foreach (var wo in overdueWos)
        {
            var daysOverdue = (DateTime.UtcNow - wo.ScheduledEndDate!.Value).Days;
            sb.AppendLine($"- [{wo.WorkOrderNumber}] {wo.Title} | Asset: {wo.Asset?.Name ?? "N/A"} | Due: {wo.ScheduledEndDate:yyyy-MM-dd} | {daysOverdue} days overdue | Assigned: {wo.AssignedTo?.FirstName ?? "Unassigned"} | Priority: {wo.Priority}");
        }

        return sb.ToString();
    }

    private async Task<string> BuildAssetHealthContextAsync(int assetId, CancellationToken ct)
    {
        var sb = new StringBuilder();

        var asset = await _unitOfWork.Assets.Query()
            .Include(a => a.Category)
            .Include(a => a.Location)
            .FirstOrDefaultAsync(a => a.Id == assetId, ct);

        if (asset == null) return "Asset not found.";

        sb.AppendLine($"=== CMMS DATA CONTEXT: ASSET HEALTH CHECK ===\n");
        sb.AppendLine($"## Asset: {asset.Name}");
        sb.AppendLine($"Tag: {asset.AssetTag} | Category: {asset.Category?.Name} | Location: {asset.Location?.Name}");
        sb.AppendLine($"Status: {asset.Status} | Criticality: {asset.Criticality}");
        sb.AppendLine($"Manufacturer: {asset.Manufacturer} | Model: {asset.Model} | Serial: {asset.SerialNumber}");
        sb.AppendLine($"Purchase Date: {asset.PurchaseDate?.ToString("yyyy-MM-dd") ?? "N/A"} | Warranty: {asset.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "N/A"}");
        sb.AppendLine($"Last Maintenance: {asset.LastMaintenanceDate?.ToString("yyyy-MM-dd") ?? "Never"} | Next: {asset.NextMaintenanceDate?.ToString("yyyy-MM-dd") ?? "Not scheduled"}");

        // All work orders for this asset
        var workOrders = await _unitOfWork.WorkOrders.Query()
            .Include(w => w.LaborEntries)
            .Where(w => w.AssetId == assetId)
            .OrderByDescending(w => w.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        sb.AppendLine($"\n## Work Order History: {workOrders.Count} total");
        var byType = workOrders.GroupBy(w => w.Type).Select(g => $"{g.Key}: {g.Count()}");
        sb.AppendLine($"By type: {string.Join(", ", byType)}");

        foreach (var wo in workOrders.Take(30))
        {
            var totalHours = wo.LaborEntries.Sum(l => l.HoursWorked);
            sb.AppendLine($"- [{wo.WorkOrderNumber}] {wo.Type} | {wo.Title} | {wo.Status} | {wo.Priority} | {wo.CreatedAt:yyyy-MM-dd} | Labor: {totalHours:F1}h");
        }

        // PM schedules for this asset
        var pmSchedules = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Where(pm => pm.AssetId == assetId && pm.IsActive)
            .ToListAsync(ct);

        sb.AppendLine($"\n## Active PM Schedules: {pmSchedules.Count}");
        foreach (var pm in pmSchedules)
        {
            sb.AppendLine($"- {pm.Name} | {pm.FrequencyType}/{pm.FrequencyValue} | Next Due: {pm.NextDueDate?.ToString("yyyy-MM-dd") ?? "N/A"} | Last Completed: {pm.LastCompletedDate?.ToString("yyyy-MM-dd") ?? "Never"}");
        }

        return sb.ToString();
    }

    // --- LLM Communication ---

    private List<object> BuildApiMessages(List<AiMessage> history, AiMessage newMessage, string? contextData)
    {
        var messages = new List<object>();

        // System prompt
        var systemContent = _settings.SystemPrompt;
        if (!string.IsNullOrEmpty(contextData))
        {
            systemContent += "\n\n" + contextData;
        }
        messages.Add(new { role = "system", content = systemContent });

        // History (limit to last 20 messages to avoid token limits)
        foreach (var msg in history.TakeLast(20))
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        // New user message
        messages.Add(new { role = newMessage.Role, content = newMessage.Content });

        return messages;
    }

    private async Task<string> CallLlmAsync(List<object> messages, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("AiAssistant");
        var requestBody = new
        {
            model = _settings.Model,
            messages,
            stream = false
        };

        _logger.LogInformation("Calling LLM at {BaseAddress}chat/completions with model {Model}", client.BaseAddress, _settings.Model);
        var response = await client.PostAsJsonAsync("chat/completions", requestBody, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("LLM returned {StatusCode}: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private async IAsyncEnumerable<string> StreamLlmAsync(List<object> messages, [EnumeratorCancellation] CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("AiAssistant");
        var requestBody = new
        {
            model = _settings.Model,
            messages,
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            string? parsedText = null;
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(data);
                var delta = json.GetProperty("choices")[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var content))
                {
                    parsedText = content.GetString();
                }
            }
            catch (JsonException)
            {
                // Skip malformed chunks
            }

            if (!string.IsNullOrEmpty(parsedText))
                yield return parsedText;
        }
    }

    private static AiConversationDto ToDto(AiConversation c) =>
        new(c.Id, c.Title, c.Summary, c.CreatedAt, c.UpdatedAt);
}
