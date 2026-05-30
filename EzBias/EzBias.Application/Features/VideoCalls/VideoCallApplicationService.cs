using EzBias.Application.Features.VideoCalls.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.VideoCalls;

public class VideoCallApplicationService : IVideoCallApplicationService
{
    private readonly ICallSessionRepository _calls;
    private readonly IConversationRepository _conversations;
    private readonly IUnitOfWork _uow;
    private readonly IVideoCallRealtime _realtime;

    public VideoCallApplicationService(
        ICallSessionRepository calls,
        IConversationRepository conversations,
        IUnitOfWork uow,
        IVideoCallRealtime realtime)
    {
        _calls = calls;
        _conversations = conversations;
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<(bool Success, string? Error, CallSessionResponse? Data)> StartCallAsync(long callerId, long conversationId, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, ct);
        if (conversation is null) return (false, "Conversation not found.", null);
        if (!IsParticipant(conversation, callerId)) return (false, "Forbidden.", null);

        var active = await _calls.GetActiveByConversationAsync(conversationId, ct);
        if (active is not null) return (false, "There is already an active call for this conversation.", null);

        var calleeId = conversation.BuyerId == callerId ? conversation.SellerId : conversation.BuyerId;
        var call = new CallSession
        {
            ConversationId = conversationId,
            CallerId = callerId,
            CalleeId = calleeId,
            Status = CallSessionStatus.Ringing,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _calls.Add(call);
        await _uow.SaveChangesAsync(ct);

        var response = ToResponse(call);
        await _realtime.PushIncomingCallAsync(calleeId, response, ct);
        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, CallSessionResponse? Data)> AcceptCallAsync(long userId, long callId, CancellationToken ct)
    {
        var call = await _calls.GetByIdAsync(callId, ct);
        if (call is null) return (false, "Call not found.", null);
        if (call.CalleeId != userId) return (false, "Forbidden.", null);
        if (call.Status != CallSessionStatus.Ringing) return (false, "Call is no longer ringing.", null);

        call.Status = CallSessionStatus.Accepted;
        call.AnsweredAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var response = ToResponse(call);
        await _realtime.PushCallAcceptedAsync(call.CallerId, response, ct);
        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, CallSessionResponse? Data)> RejectCallAsync(long userId, long callId, CancellationToken ct)
    {
        var call = await _calls.GetByIdAsync(callId, ct);
        if (call is null) return (false, "Call not found.", null);
        if (call.CalleeId != userId) return (false, "Forbidden.", null);
        if (call.Status != CallSessionStatus.Ringing) return (false, "Call is no longer ringing.", null);

        call.Status = CallSessionStatus.Rejected;
        call.EndedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var response = ToResponse(call);
        await _realtime.PushCallRejectedAsync(call.CallerId, response, ct);
        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, CallSessionResponse? Data)> EndCallAsync(long userId, long callId, CancellationToken ct)
    {
        var call = await _calls.GetByIdAsync(callId, ct);
        if (call is null) return (false, "Call not found.", null);
        if (call.CallerId != userId && call.CalleeId != userId) return (false, "Forbidden.", null);
        if (call.Status is not (CallSessionStatus.Ringing or CallSessionStatus.Accepted))
            return (false, "Call has already ended.", null);

        call.Status = CallSessionStatus.Ended;
        call.EndedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var response = ToResponse(call);
        var recipientId = call.CallerId == userId ? call.CalleeId : call.CallerId;
        await _realtime.PushCallEndedAsync(recipientId, response, ct);
        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, IReadOnlyList<CallSessionResponse>? Data)> GetConversationCallsAsync(long userId, long conversationId, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, ct);
        if (conversation is null) return (false, "Conversation not found.", null);
        if (!IsParticipant(conversation, userId)) return (false, "Forbidden.", null);

        var calls = await _calls.GetByConversationAsync(conversationId, ct);
        return (true, null, calls.Select(ToResponse).ToList());
    }

    private static bool IsParticipant(Conversation conversation, long userId)
        => conversation.BuyerId == userId || conversation.SellerId == userId;

    private static CallSessionResponse ToResponse(CallSession call)
        => new(
            call.Id,
            call.ConversationId,
            call.CallerId,
            call.CalleeId,
            call.Status.ToString(),
            call.CreatedAt,
            call.AnsweredAt,
            call.EndedAt);
}
