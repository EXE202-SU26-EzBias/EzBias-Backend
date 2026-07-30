using System.Security.Claims;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Chat;
using EzBias.Application.Features.Chat.Dtos;
using EzBias.Application.Features.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IChatApplicationService _chat;
    private readonly IImageUploader _imageUploader;

    public ConversationsController(IChatApplicationService chat, IImageUploader imageUploader)
    {
        _chat = chat;
        _imageUploader = imageUploader;
    }

    /// <summary>POST /api/conversations — start or resume a conversation</summary>
    [HttpPost]
    public async Task<IActionResult> StartOrGet([FromBody] StartConversationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _chat.StartOrGetConversationAsync(userId, request, ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result, notFoundAsBadRequest: true);
        return Ok(result.Value);
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return CreatedAtAction(nameof(GetMessages), new { id }, result.Value);
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    /// <summary>PUT /api/conversations/{id}/read — mark messages as read</summary>
    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _chat.MarkAsReadAsync(userId, id, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result);
        return NoContent();
    }

    /// <summary>POST /api/conversations/{id}/upload-image — upload chat image</summary>
    [HttpPost("{id:long}/upload-image")]
    [RequestSizeLimit(5_242_880)] // 5MB limit
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadImage(
        [FromRoute] long id,
        IFormFile image,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        // Verify user is participant
        var conversations = await _chat.GetMyConversationsAsync(userId, ct);
        if (!conversations.Any(c => c.Id == id))
            return Forbid();

        // Validate file
        if (image == null || image.Length == 0)
            return BadRequest(new { message = "No image file provided." });

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(image.ContentType.ToLower()))
            return BadRequest(new { message = "Only JPEG, PNG, GIF, and WebP images are allowed." });

        if (image.Length > 5_242_880) // 5MB
            return BadRequest(new { message = "Image size cannot exceed 5MB." });

        try
        {
            await using var stream = image.OpenReadStream();
            var imageUrl = await _imageUploader.UploadProductImageAsync(
                new UploadFile(
                    stream,
                    image.FileName,
                    image.ContentType,
                    image.Length),
                ct);
            return Ok(new { imageUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Image upload failed: {ex.Message}" });
        }
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
