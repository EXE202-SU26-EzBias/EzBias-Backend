using System.Security.Claims;
using EzBias.Application.Features.Chat;
using EzBias.Application.Features.Chat.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IChatApplicationService _chat;

    public ConversationsController(IChatApplicationService chat) => _chat = chat;

    /// <summary>POST /api/conversations — start or resume a conversation</summary>
    [HttpPost]
    public async Task<IActionResult> StartOrGet([FromBody] StartConversationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _chat.StartOrGetConversationAsync(userId, request, ct);
        if (!result.Success || result.Data is null) return BadRequest(new { message = result.Error });
        return Ok(result.Data);
    }

    /// <summary>GET /api/conversations — list my conversations</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _chat.GetMyConversationsAsync(userId, ct);
        return Ok(data);
    }

    /// <summary>POST /api/conversations/{id}/messages — send a message</summary>
    [HttpPost("{id:long}/messages")]
    public async Task<IActionResult> SendMessage(
        [FromRoute] long id,
        [FromBody] SendMessageRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _chat.SendMessageAsync(userId, id, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Conversation not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return CreatedAtAction(nameof(GetMessages), new { id }, result.Data);
    }

    /// <summary>GET /api/conversations/{id}/messages — load message history</summary>
    [HttpGet("{id:long}/messages")]
    public async Task<IActionResult> GetMessages(
        [FromRoute] long id,
        [FromQuery] long? before,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _chat.GetMessagesAsync(userId, id, before, pageSize, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Conversation not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    /// <summary>PUT /api/conversations/{id}/read — mark messages as read</summary>
    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _chat.MarkAsReadAsync(userId, id, ct);
        if (!result.Success)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Conversation not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
