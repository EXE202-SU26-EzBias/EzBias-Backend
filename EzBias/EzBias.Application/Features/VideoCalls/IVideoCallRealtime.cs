using EzBias.Application.Features.VideoCalls.Dtos;

namespace EzBias.Application.Features.VideoCalls;

public interface IVideoCallRealtime
{
    Task PushIncomingCallAsync(long calleeId, CallSessionResponse call, CancellationToken ct = default);
    Task PushCallAcceptedAsync(long callerId, CallSessionResponse call, CancellationToken ct = default);
    Task PushCallRejectedAsync(long callerId, CallSessionResponse call, CancellationToken ct = default);
    Task PushCallEndedAsync(long recipientId, CallSessionResponse call, CancellationToken ct = default);
}
