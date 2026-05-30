namespace EzBias.Application.Features.VideoCalls.Dtos;

public record CallSessionResponse(
    long Id,
    long ConversationId,
    long CallerId,
    long CalleeId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AnsweredAt,
    DateTimeOffset? EndedAt);
