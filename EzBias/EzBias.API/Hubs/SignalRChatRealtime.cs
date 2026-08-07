using EzBias.Application.Features.Chat;
using EzBias.Application.Features.Chat.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

public sealed class SignalRChatRealtime : IChatRealtime
{
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<SignalRChatRealtime> _logger;

    public SignalRChatRealtime(
        IHubContext<ChatHub> hub,
        ILogger<SignalRChatRealtime> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PushMessageAsync(
        long recipientId,
        MessageResponse message,
        CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients
                .Group(ChatHub.UserGroup(recipientId))
                .SendAsync("ReceiveMessage", message, ct);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                ex,
                "ReceiveMessage broadcast canceled for conversation {ConversationId}, message {MessageId}.",
                message.ConversationId,
                message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "ReceiveMessage broadcast failed for conversation {ConversationId}, message {MessageId}.",
                message.ConversationId,
                message.Id);
        }
    }

    public async Task PushConversationReadAsync(
        long senderId,
        long conversationId,
        long readByUserId,
        CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients
                .Group(ChatHub.UserGroup(senderId))
                .SendAsync("ConversationRead", new { conversationId, readByUserId }, ct);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                ex,
                "ConversationRead broadcast canceled for conversation {ConversationId}.",
                conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "ConversationRead broadcast failed for conversation {ConversationId}.",
                conversationId);
        }
    }
}
