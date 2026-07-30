using EzBias.Application.Common.Results;
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

    public async Task<Result<CallSessionResponse>> StartCallAsync(long callerId, long conversationId, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, ct);
        if (conversation is null) return Result<CallSessionResponse>.Fail("Conversation not found.", ApplicationErrorCode.ResourceNotFound);
        if (!IsParticipant(conversation, callerId)) return Result<CallSessionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var active = await _calls.GetActiveByConversationAsync(conversationId, ct);
        if (active is not null) return Result<CallSessionResponse>.Fail("There is already an active call for this conversation.", ApplicationErrorCode.Validation);

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
        return Result<CallSessionResponse>.Ok(response);
    }

    public async Task<Result<CallSessionResponse>> AcceptCallAsync(long userId, long callId, CancellationToken ct)
    {
        var call = await _calls.GetByIdAsync(callId, ct);
        if (call is null) return Result<CallSessionResponse>.Fail("Call not found.", ApplicationErrorCode.ResourceNotFound);
        if (call.CalleeId != userId) return Result<CallSessionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (call.Status != CallSessionStatus.Ringing) return Result<CallSessionResponse>.Fail("Call is no longer ringing.", ApplicationErrorCode.Validation);

        call.Status = CallSessionStatus.Accepted;
        call.AnsweredAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var response = ToResponse(call);
        await _realtime.PushCallAcceptedAsync(call.CallerId, response, ct);
        return Result<CallSessionResponse>.Ok(response);
    }

    public async Task<Result<CallSessionResponse>> RejectCallAsync(long userId, long callId, CancellationToken ct)
    {
        var call = await _calls.GetByIdAsync(callId, ct);
        if (call is null) return Result<CallSessionResponse>.Fail("Call not found.", ApplicationErrorCode.ResourceNotFound);
        if (call.CalleeId != userId) return Result<CallSessionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (call.Status != CallSessionStatus.Ringing) return Result<CallSessionResponse>.Fail("Call is no longer ringing.", ApplicationErrorCode.Validation);

        call.Status = CallSessionStatus.Rejected;
        call.EndedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var response = ToResponse(call);
        await _realtime.PushCallRejectedAsync(call.CallerId, response, ct);
        return Result<CallSessionResponse>.Ok(response);
    }

    public async Task<Result<CallSessionResponse>> EndCallAsync(long userId, long callId, CancellationToken ct)
    {
        var call = await _calls.GetByIdAsync(callId, ct);
        if (call is null) return Result<CallSessionResponse>.Fail("Call not found.", ApplicationErrorCode.ResourceNotFound);
        if (call.CallerId != userId && call.CalleeId != userId) return Result<CallSessionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (call.Status is not (CallSessionStatus.Ringing or CallSessionStatus.Accepted))
            return Result<CallSessionResponse>.Fail("Call has already ended.", ApplicationErrorCode.Validation);

        call.Status = CallSessionStatus.Ended;
        call.EndedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var response = ToResponse(call);
        var recipientId = call.CallerId == userId ? call.CalleeId : call.CallerId;
        await _realtime.PushCallEndedAsync(recipientId, response, ct);
        return Result<CallSessionResponse>.Ok(response);
    }

    public async Task<Result<IReadOnlyList<CallSessionResponse>>> GetConversationCallsAsync(long userId, long conversationId, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, ct);
        if (conversation is null) return Result<IReadOnlyList<CallSessionResponse>>.Fail("Conversation not found.", ApplicationErrorCode.ResourceNotFound);
        if (!IsParticipant(conversation, userId)) return Result<IReadOnlyList<CallSessionResponse>>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var calls = await _calls.GetByConversationAsync(conversationId, ct);
        return Result<IReadOnlyList<CallSessionResponse>>.Ok(calls.Select(ToResponse).ToList());
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
