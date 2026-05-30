using System.Security.Claims;
using System.Text.Json;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

[Authorize]
public class CallHub : Hub
{
    private readonly ICallSessionRepository _calls;

    public CallHub(ICallSessionRepository calls) => _calls = calls;

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId.Value));

        await base.OnDisconnectedAsync(exception);
    }

    public Task SendOffer(long callId, long targetUserId, JsonElement offer)
        => SendSignalAsync("WebRtcOffer", callId, targetUserId, new { callId, offer });

    public Task SendAnswer(long callId, long targetUserId, JsonElement answer)
        => SendSignalAsync("WebRtcAnswer", callId, targetUserId, new { callId, answer });

    public Task SendIceCandidate(long callId, long targetUserId, JsonElement candidate)
        => SendSignalAsync("IceCandidate", callId, targetUserId, new { callId, candidate });

    public static string UserGroup(long userId) => $"call-user-{userId}";

    private async Task SendSignalAsync(string eventName, long callId, long targetUserId, object payload)
    {
        var userId = GetUserId() ?? throw new HubException("Unauthorized.");
        var call = await _calls.GetByIdAsync(callId, Context.ConnectionAborted)
            ?? throw new HubException("Call not found.");

        if (call.CallerId != userId && call.CalleeId != userId)
            throw new HubException("Forbidden.");

        var expectedTargetId = call.CallerId == userId ? call.CalleeId : call.CallerId;
        if (targetUserId != expectedTargetId)
            throw new HubException("Invalid signal target.");

        if (call.Status is not (CallSessionStatus.Ringing or CallSessionStatus.Accepted))
            throw new HubException("Call has already ended.");

        await Clients.Group(UserGroup(targetUserId)).SendAsync(
            eventName,
            new { callId, fromUserId = userId, payload },
            Context.ConnectionAborted);
    }

    private long? GetUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub");
        return long.TryParse(sub, out var id) ? id : null;
    }
}
