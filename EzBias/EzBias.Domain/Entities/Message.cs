namespace EzBias.Domain.Entities;

public class Message
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; } = false;

    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
