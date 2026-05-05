using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class EscrowTransaction
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long SellerId { get; set; }
    public EscrowType Type { get; set; }
    public decimal Amount { get; set; }
    public long? PaymentId { get; set; }
    public long? PayoutId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Order Order { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Payout? Payout { get; set; }
}
