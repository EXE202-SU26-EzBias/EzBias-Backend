using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Dispute
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long InitiatorId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? AdminNote { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Order Order { get; set; } = null!;
    public User Initiator { get; set; } = null!;
    public ICollection<DisputeItem> Items { get; set; } = new List<DisputeItem>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
