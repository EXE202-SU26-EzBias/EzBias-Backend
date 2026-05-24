using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Payment
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public PaymentType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string Reference { get; set; } = string.Empty;

    public string? TransferContent { get; set; }
    public string? ProviderTxnId { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
    public ICollection<EscrowTransaction> EscrowTransactions { get; set; } = new List<EscrowTransaction>();
    public ICollection<CommissionTransaction> CommissionTransactions { get; set; } = new List<CommissionTransaction>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
