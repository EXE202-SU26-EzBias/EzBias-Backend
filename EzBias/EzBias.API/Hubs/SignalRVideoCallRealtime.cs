using EzBias.Application.Features.VideoCalls;
using EzBias.Application.Features.VideoCalls.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

public sealed class SignalRVideoCallRealtime : IVideoCallRealtime
{
    private readonly IHubContext<CallHub> _hub;

    public SignalRVideoCallRealtime(IHubContext<CallHub> hub) => _hub = hub;

    public Task PushIncomingCallAsync(long calleeId, CallSessionResponse call, CancellationToken ct = default)
        => _hub.Clients.Group(CallHub.UserGroup(calleeId)).SendAsync("IncomingCall", call, ct);

    public Task PushCallAcceptedAsync(long callerId, CallSessionResponse call, CancellationToken ct = default)
        => _hub.Clients.Group(CallHub.UserGroup(callerId)).SendAsync("CallAccepted", call, ct);

    public Task PushCallRejectedAsync(long callerId, CallSessionResponse call, CancellationToken ct = default)
        => _hub.Clients.Group(CallHub.UserGroup(callerId)).SendAsync("CallRejected", call, ct);

    public Task PushCallEndedAsync(long recipientId, CallSessionResponse call, CancellationToken ct = default)
        => _hub.Clients.Group(CallHub.UserGroup(recipientId)).SendAsync("CallEnded", call, ct);
}
