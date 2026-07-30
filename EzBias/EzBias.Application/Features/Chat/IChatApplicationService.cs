using EzBias.Application.Common.Results;
using EzBias.Application.Features.Chat.Dtos;

namespace EzBias.Application.Features.Chat;

public interface IChatApplicationService
{
    Task<Result<ConversationSummary>>
        StartOrGetConversationAsync(long callerId, StartConversationRequest request, CancellationToken ct);

    Task<IReadOnlyList<ConversationSummary>>
        GetMyConversationsAsync(long userId, CancellationToken ct);

    Task<Result<MessageResponse>>
        SendMessageAsync(long senderId, long conversationId, SendMessageRequest request, CancellationToken ct);

    Task<Result<MessagePageResponse>>
        GetMessagesAsync(long userId, long conversationId, long? before, int pageSize, CancellationToken ct);

    Task<Result>
        MarkAsReadAsync(long userId, long conversationId, CancellationToken ct);
}
