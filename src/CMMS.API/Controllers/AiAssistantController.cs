using System.Security.Claims;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiAssistantController : ControllerBase
{
    private readonly IAiAssistantService _aiService;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(IAiAssistantService aiService, ILogger<AiAssistantController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await _aiService.GetStatusAsync(ct);
        return Ok(status);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var userId = GetUserId();
        var conversations = await _aiService.GetUserConversationsAsync(userId, ct);
        return Ok(new { success = true, data = conversations });
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest? request, CancellationToken ct)
    {
        var userId = GetUserId();
        var conversation = await _aiService.CreateConversationAsync(userId, request?.Title, ct);
        return Ok(new { success = true, data = conversation });
    }

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var conversation = await _aiService.GetConversationAsync(id, userId, ct);
        if (conversation == null) return NotFound();
        return Ok(new { success = true, data = conversation });
    }

    [HttpDelete("conversations/{id}")]
    public async Task<IActionResult> DeleteConversation(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _aiService.DeleteConversationAsync(id, userId, ct);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPut("conversations/{id}/title")]
    public async Task<IActionResult> RenameConversation(int id, [FromBody] RenameConversationRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _aiService.RenameConversationAsync(id, userId, request.Title, ct);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        try
        {
            var message = await _aiService.SendMessageAsync(id, userId, request.Message, request.ContextType, request.AssetId, ct);
            return Ok(new { success = true, data = message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, errors = new[] { ex.Message } });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with AI service");
            return StatusCode(502, new { success = false, errors = new[] { "AI service is unavailable" } });
        }
    }

    [HttpPost("conversations/{id}/stream")]
    public async Task StreamMessage(int id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var userId = GetUserId();

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var chunk in _aiService.StreamMessageAsync(id, userId, request.Message, request.ContextType, request.AssetId, ct))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { content = chunk })}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to stream from AI service");
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "AI service is unavailable" })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
