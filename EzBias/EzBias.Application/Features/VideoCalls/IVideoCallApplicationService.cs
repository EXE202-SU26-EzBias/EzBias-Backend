using EzBias.Application.Common.Results;
using EzBias.Application.Features.VideoCalls.Dtos;

namespace EzBias.Application.Features.VideoCalls;

public interface IVideoCallApplicationService
{
    Task<Result<CallSessionResponse>> StartCallAsync(long callerId, long conversationId, CancellationToken ct);
    Task<Result<CallSessionResponse>> AcceptCallAsync(long userId, long callId, CancellationToken ct);
    Task<Result<CallSessionResponse>> RejectCallAsync(long userId, long callId, CancellationToken ct);
    Task<Result<CallSessionResponse>> EndCallAsync(long userId, long callId, CancellationToken ct);
    Task<Result<IReadOnlyList<CallSessionResponse>>> GetConversationCallsAsync(long userId, long conversationId, CancellationToken ct);
}
