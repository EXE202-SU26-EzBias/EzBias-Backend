using EzBias.Application.Features.VideoCalls;
using EzBias.Application.Features.VideoCalls.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

public sealed class SignalRVideoCallRealtime : IVideoCallRealtime
{
    private readonly IHubContext<CallHub> _hub;
    private readonly ILogger<SignalRVideoCallRealtime> _logger;

    public SignalRVideoCallRealtime(
        IHubContext<CallHub> hub,
        ILogger<SignalRVideoCallRealtime> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PushIncomingCallAsync(
        long calleeId,
        CallSessionResponse call,
        CancellationToken ct = default)
    {
        await SendAsync(
            "IncomingCall",
            calleeId,
            call,
            "IncomingCall broadcast failed for call {CallId}.",
            ct);
    }

    public async Task PushCallAcceptedAsync(
        long callerId,
        CallSessionResponse call,
        CancellationToken ct = default)
    {
        await SendAsync(
            "CallAccepted",
            callerId,
            call,
            "CallAccepted broadcast failed for call {CallId}.",
            ct);
    }

    public async Task PushCallRejectedAsync(
        long callerId,
        CallSessionResponse call,
        CancellationToken ct = default)
    {
        await SendAsync(
            "CallRejected",
            callerId,
            call,
            "CallRejected broadcast failed for call {CallId}.",
            ct);
    }

    public async Task PushCallEndedAsync(
        long recipientId,
        CallSessionResponse call,
        CancellationToken ct = default)
    {
        await SendAsync(
            "CallEnded",
            recipientId,
            call,
            "CallEnded broadcast failed for call {CallId}.",
            ct);
    }

    private async Task SendAsync(
        string eventName,
        long recipientId,
        CallSessionResponse call,
        string failureMessage,
        CancellationToken ct)
    {
        try
        {
            await _hub.Clients
                .Group(CallHub.UserGroup(recipientId))
                .SendAsync(eventName, call, ct);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                ex,
                "{EventName} broadcast canceled for call {CallId}.",
                eventName,
                call.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                failureMessage,
                call.Id);
        }
    }
}
