using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class CallSession
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long CallerId { get; set; }
    public long CalleeId { get; set; }
    public CallSessionStatus Status { get; set; } = CallSessionStatus.Ringing;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AnsweredAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User Caller { get; set; } = null!;
    public User Callee { get; set; } = null!;
}
