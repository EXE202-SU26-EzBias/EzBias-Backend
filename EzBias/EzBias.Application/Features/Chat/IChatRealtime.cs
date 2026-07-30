using EzBias.Application.Features.Chat.Dtos;

namespace EzBias.Application.Features.Chat;

public interface IChatRealtime
{
    Task PushMessageAsync(long recipientId, MessageResponse message, CancellationToken ct = default);
    Task PushConversationReadAsync(long senderId, long conversationId, long readByUserId, CancellationToken ct = default);
}
