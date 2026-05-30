using EzBias.Application.Features.VideoCalls.Dtos;

namespace EzBias.Application.Features.VideoCalls;

public interface IVideoCallApplicationService
{
    Task<(bool Success, string? Error, CallSessionResponse? Data)> StartCallAsync(long callerId, long conversationId, CancellationToken ct);
    Task<(bool Success, string? Error, CallSessionResponse? Data)> AcceptCallAsync(long userId, long callId, CancellationToken ct);
    Task<(bool Success, string? Error, CallSessionResponse? Data)> RejectCallAsync(long userId, long callId, CancellationToken ct);
    Task<(bool Success, string? Error, CallSessionResponse? Data)> EndCallAsync(long userId, long callId, CancellationToken ct);
    Task<(bool Success, string? Error, IReadOnlyList<CallSessionResponse>? Data)> GetConversationCallsAsync(long userId, long conversationId, CancellationToken ct);
}
