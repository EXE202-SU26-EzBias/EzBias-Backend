using EzBias.Application.Features.Chat;
using EzBias.Application.Features.Chat.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

public sealed class SignalRChatRealtime : IChatRealtime
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRChatRealtime(IHubContext<ChatHub> hub) => _hub = hub;

    public Task PushMessageAsync(long recipientId, MessageResponse message, CancellationToken ct = default)
        => _hub.Clients
            .Group(ChatHub.UserGroup(recipientId))
            .SendAsync("ReceiveMessage", message, ct);

    public Task PushConversationReadAsync(long senderId, long conversationId, long readByUserId, CancellationToken ct = default)
        => _hub.Clients
            .Group(ChatHub.UserGroup(senderId))
            .SendAsync("ConversationRead", new { conversationId, readByUserId }, ct);
}
