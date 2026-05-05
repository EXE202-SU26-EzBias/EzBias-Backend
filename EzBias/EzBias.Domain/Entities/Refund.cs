using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Refund
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public long? OrderId { get; set; }
    public long? DisputeId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? ProviderRef { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Payment Payment { get; set; } = null!;
    public Order? Order { get; set; }
    public Dispute? Dispute { get; set; }
}
