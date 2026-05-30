using System.Security.Claims;
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
        if (!result.Success || result.Data is null) return ToErrorResult(result.Error);

        return Ok(result.Data);
    }

    [HttpGet("conversations/{conversationId:long}/calls")]
    public async Task<IActionResult> GetConversationCalls([FromRoute] long conversationId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.GetConversationCallsAsync(userId, conversationId, ct);
        if (!result.Success || result.Data is null) return ToErrorResult(result.Error);

        return Ok(result.Data);
    }

    [HttpPost("calls/{callId:long}/accept")]
    public async Task<IActionResult> AcceptCall([FromRoute] long callId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.AcceptCallAsync(userId, callId, ct);
        if (!result.Success || result.Data is null) return ToErrorResult(result.Error);

        return Ok(result.Data);
    }

    [HttpPost("calls/{callId:long}/reject")]
    public async Task<IActionResult> RejectCall([FromRoute] long callId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.RejectCallAsync(userId, callId, ct);
        if (!result.Success || result.Data is null) return ToErrorResult(result.Error);

        return Ok(result.Data);
    }

    [HttpPost("calls/{callId:long}/end")]
    public async Task<IActionResult> EndCall([FromRoute] long callId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _videoCalls.EndCallAsync(userId, callId, ct);
        if (!result.Success || result.Data is null) return ToErrorResult(result.Error);

        return Ok(result.Data);
    }

    private ObjectResult ToErrorResult(string? error)
        => error switch
        {
            "Forbidden." => StatusCode(StatusCodes.Status403Forbidden, new { message = error }),
            "Conversation not found." => NotFound(new { message = error }),
            "Call not found." => NotFound(new { message = error }),
            _ => BadRequest(new { message = error })
        };

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
