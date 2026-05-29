using EzBias.Application.Features.Chat.Dtos;

namespace EzBias.Application.Features.Chat;

/// <summary>
/// Abstraction over SignalR so Application layer has no dependency on Microsoft.AspNetCore.SignalR.
/// Implemented in the API layer by SignalRChatRealtime.
/// </summary>
public interface IChatRealtime
{
    Task PushMessageAsync(long recipientId, MessageResponse message, CancellationToken ct = default);
    Task PushConversationReadAsync(long senderId, long conversationId, long readByUserId, CancellationToken ct = default);
}
