using System.Security.Claims;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.VideoCalls;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class VideoCallsController : ControllerBase
{
    private readonly IVideoCallApplicationService _videoCalls;

    public VideoCallsController(IVideoCallApplicationService videoCalls) => _videoCalls = videoCalls;

    [HttpPost("conversations/{conversationId:long}/calls")]
    public async Task<IActionResult> StartCall([FromRoute] long conversationId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.StartCallAsync(userId, conversationId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);

        return Ok(result.Value);
    }

    [HttpGet("conversations/{conversationId:long}/calls")]
    public async Task<IActionResult> GetConversationCalls([FromRoute] long conversationId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.GetConversationCallsAsync(userId, conversationId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);

        return Ok(result.Value);
    }

    [HttpPost("calls/{callId:long}/accept")]
    public async Task<IActionResult> AcceptCall([FromRoute] long callId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.AcceptCallAsync(userId, callId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);

        return Ok(result.Value);
    }

    [HttpPost("calls/{callId:long}/reject")]
    public async Task<IActionResult> RejectCall([FromRoute] long callId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.RejectCallAsync(userId, callId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);

        return Ok(result.Value);
    }

    [HttpPost("calls/{callId:long}/end")]
    public async Task<IActionResult> EndCall([FromRoute] long callId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.EndCallAsync(userId, callId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);

        return Ok(result.Value);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
