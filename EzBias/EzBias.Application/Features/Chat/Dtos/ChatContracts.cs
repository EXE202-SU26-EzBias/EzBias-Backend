namespace EzBias.Application.Features.Chat.Dtos;

public record StartConversationRequest(long CounterpartId, long? ProductId, long? OrderId);
public record SendMessageRequest(string Content);

public record ConversationSummary(
    long Id,
    long OtherParticipantId,
    string OtherParticipantName,
    string OtherParticipantAvatarUrl,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    long? ProductId,
    long? OrderId);

public record MessageResponse(
    long Id,
    long ConversationId,
    long SenderId,
    string SenderName,
    string SenderAvatarUrl,
    string Content,
    DateTimeOffset SentAt,
    bool IsRead);

public record MessagePageResponse(
    IReadOnlyList<MessageResponse> Messages,
    bool HasMore,
    long? NextCursor);
