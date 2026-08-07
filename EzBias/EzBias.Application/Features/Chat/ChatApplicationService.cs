using EzBias.Application.Common.Results;
using EzBias.Application.Features.Chat.Dtos;
using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Chat;

public class ChatApplicationService : IChatApplicationService
{
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IUserRepository _users;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;
    private readonly IChatRealtime _chatRealtime;

    public ChatApplicationService(
        IConversationRepository conversations,
        IMessageRepository messages,
        IUserRepository users,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow,
        IChatRealtime chatRealtime)
    {
        _conversations = conversations;
        _messages = messages;
        _users = users;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
        _chatRealtime = chatRealtime;
    }

    public async Task<Result<ConversationSummary>>
        StartOrGetConversationAsync(long callerId, StartConversationRequest request, CancellationToken ct)
    {
        if (callerId == request.CounterpartId)
            return Result<ConversationSummary>.Fail("Cannot start a conversation with yourself.", ApplicationErrorCode.Validation);

        var counterpart = await _users.GetByIdAsync(request.CounterpartId, ct);
        if (counterpart is null || counterpart.DeletedAt != null)
            return Result<ConversationSummary>.Fail("User not found.", ApplicationErrorCode.ResourceNotFound);

        var caller = await _users.GetByIdAsync(callerId, ct);
        if (caller is null) return Result<ConversationSummary>.Fail("Unauthorized.", ApplicationErrorCode.Unauthorized);

        long buyerId = callerId;
        long sellerId = request.CounterpartId;

        var existing = await _conversations.GetByParticipantsAsync(buyerId, sellerId, ct);
        if (existing is not null)
            return Result<ConversationSummary>.Ok(await ToSummaryAsync(existing, callerId, ct));

        var conversation = new Conversation
        {
            BuyerId = buyerId,
            SellerId = sellerId,
            ProductId = request.ProductId,
            OrderId = request.OrderId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastMessageAt = DateTimeOffset.UtcNow
        };

        _conversations.Add(conversation);
        await _uow.SaveChangesAsync(ct);

        var created = await _conversations.GetByIdAsync(conversation.Id, ct);
        return Result<ConversationSummary>.Ok(await ToSummaryAsync(created!, callerId, ct));
    }

    public async Task<IReadOnlyList<ConversationSummary>> GetMyConversationsAsync(long userId, CancellationToken ct)
    {
        var list = await _conversations.GetByUserAsync(userId, ct);
        var summaries = new List<ConversationSummary>();
        foreach (var c in list)
            summaries.Add(await ToSummaryAsync(c, userId, ct));
        return summaries;
    }

    public async Task<Result<MessageResponse>>
        SendMessageAsync(long senderId, long conversationId, SendMessageRequest request, CancellationToken ct)
    {
        var trimmed = (request.Content ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result<MessageResponse>.Fail("Message content cannot be empty.", ApplicationErrorCode.Validation);
        if (trimmed.Length > 2000)
            return Result<MessageResponse>.Fail("Message content cannot exceed 2000 characters.", ApplicationErrorCode.Validation);

        var conversation = await _conversations.GetByIdAsync(conversationId, ct);
        if (conversation is null) return Result<MessageResponse>.Fail("Conversation not found.", ApplicationErrorCode.ResourceNotFound);
        if (conversation.BuyerId != senderId && conversation.SellerId != senderId)
            return Result<MessageResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var sender = await _users.GetByIdAsync(senderId, ct);
        if (sender is null) return Result<MessageResponse>.Fail("Unauthorized.", ApplicationErrorCode.Unauthorized);

        var recipientId = conversation.BuyerId == senderId ? conversation.SellerId : conversation.BuyerId;

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = trimmed,
            SentAt = DateTimeOffset.UtcNow,
            IsRead = false
        };

        _messages.Add(message);
        conversation.LastMessageAt = message.SentAt;

        var preview = IsImageUrl(trimmed) 
            ? "📷 Photo" 
            : (trimmed.Length > 100 ? trimmed[..100] : trimmed);
        _notifications.Add(_notificationFactory.NewMessage(recipientId, conversationId, sender.Username, preview));

        await _uow.SaveChangesAsync(ct);

        var response = ToMessageResponse(message, sender);
        await _chatRealtime.PushMessageAsync(recipientId, response, ct);

        return Result<MessageResponse>.Ok(response);
    }

    public async Task<Result<MessagePageResponse>>
        GetMessagesAsync(long userId, long conversationId, long? before, int pageSize, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, ct);
        if (conversation is null) return Result<MessagePageResponse>.Fail("Conversation not found.", ApplicationErrorCode.ResourceNotFound);
        if (conversation.BuyerId != userId && conversation.SellerId != userId)
            return Result<MessagePageResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var size = Math.Clamp(pageSize, 1, 100);
        var msgs = await _messages.GetPageAsync(conversationId, before, size + 1, ct);

        var hasMore = msgs.Count > size;
        var page = hasMore ? msgs.Take(size).ToList() : msgs.ToList();

        var userIds = page.Select(m => m.SenderId).Distinct().ToList();
        var userMap = new Dictionary<long, User>();
        foreach (var uid in userIds)
        {
            var u = await _users.GetByIdAsync(uid, ct);
            if (u is not null) userMap[uid] = u;
        }

        var responses = page.Select(m =>
        {
            userMap.TryGetValue(m.SenderId, out var s);
            return ToMessageResponse(m, s);
        }).ToList();

        long? nextCursor = hasMore ? page.First().Id : null;
        return Result<MessagePageResponse>.Ok(new MessagePageResponse(responses, hasMore, nextCursor));
    }

    public async Task<Result>
        MarkAsReadAsync(long userId, long conversationId, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, ct);
        if (conversation is null) return Result.Fail("Conversation not found.", ApplicationErrorCode.ResourceNotFound);
        if (conversation.BuyerId != userId && conversation.SellerId != userId)
            return Result.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var unread = await _messages.GetUnreadByRecipientAsync(conversationId, userId, ct);
        if (unread.Count == 0) return Result.Ok();

        var senderId = unread[0].SenderId;
        foreach (var msg in unread)
            msg.IsRead = true;

        await _uow.SaveChangesAsync(ct);
        await _chatRealtime.PushConversationReadAsync(senderId, conversationId, userId, ct);

        return Result.Ok();
    }

    private async Task<ConversationSummary> ToSummaryAsync(Conversation c, long callerId, CancellationToken ct)
    {
        var otherId = c.BuyerId == callerId ? c.SellerId : c.BuyerId;
        var other = c.BuyerId == callerId ? c.Seller : c.Buyer;

        var lastMsgs = await _messages.GetPageAsync(c.Id, null, 1, ct);
        var lastMsg = lastMsgs.FirstOrDefault();

        var preview = lastMsg?.Content is { } content
            ? IsImageUrl(content) 
                ? "📷 Photo" 
                : (content.Length > 100 ? content[..100] : content)
            : null;

        var unread = await _messages.CountUnreadAsync(c.Id, callerId, ct);

        return new ConversationSummary(
            c.Id,
            otherId,
            other?.FullName ?? other?.Username ?? string.Empty,
            other?.AvatarUrl ?? string.Empty,
            preview,
            lastMsg?.SentAt ?? c.LastMessageAt,
            unread,
            c.ProductId,
            c.OrderId);
    }

    private static bool IsImageUrl(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        if (content.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            return true;

        var lowerContent = content.ToLowerInvariant();
        return lowerContent.StartsWith("http://") || lowerContent.StartsWith("https://")
            && (lowerContent.Contains(".jpg") || lowerContent.Contains(".jpeg") || 
                lowerContent.Contains(".png") || lowerContent.Contains(".gif") || 
                lowerContent.Contains(".webp"));
    }

    private static MessageResponse ToMessageResponse(Message m, User? sender)
        => new(m.Id, m.ConversationId, m.SenderId,
            sender?.FullName ?? sender?.Username ?? string.Empty,
            sender?.AvatarUrl ?? string.Empty,
            m.Content, m.SentAt, m.IsRead);
}
