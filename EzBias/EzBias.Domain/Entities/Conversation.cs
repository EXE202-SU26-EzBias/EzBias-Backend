namespace EzBias.Domain.Entities;

public class Conversation
{
    public long Id { get; set; }
    public long BuyerId { get; set; }
    public long SellerId { get; set; }
    public long? ProductId { get; set; }
    public long? OrderId { get; set; }
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Buyer { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
