using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Payout
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long SellerId { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string? BankTransferRef { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public ICollection<EscrowTransaction> EscrowTransactions { get; set; } = new List<EscrowTransaction>();
}
