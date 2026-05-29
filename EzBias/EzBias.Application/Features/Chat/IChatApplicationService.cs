using EzBias.Application.Features.Chat.Dtos;

namespace EzBias.Application.Features.Chat;

public interface IChatApplicationService
{
    Task<(bool Success, string? Error, ConversationSummary? Data)>
        StartOrGetConversationAsync(long callerId, StartConversationRequest request, CancellationToken ct);

    Task<IReadOnlyList<ConversationSummary>>
        GetMyConversationsAsync(long userId, CancellationToken ct);

    Task<(bool Success, string? Error, MessageResponse? Data)>
        SendMessageAsync(long senderId, long conversationId, SendMessageRequest request, CancellationToken ct);

    Task<(bool Success, string? Error, MessagePageResponse? Data)>
        GetMessagesAsync(long userId, long conversationId, long? before, int pageSize, CancellationToken ct);

    Task<(bool Success, string? Error)>
        MarkAsReadAsync(long userId, long conversationId, CancellationToken ct);
}
