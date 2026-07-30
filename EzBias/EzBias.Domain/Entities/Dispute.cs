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

    public TransitionOutcome Open(DateTimeOffset now)
    {
        if (Status == DisputeStatus.Open)
            return TransitionOutcome.NoOp;
        if (Status is not (DisputeStatus.ResolvedSeller or DisputeStatus.ResolvedBuyer))
            return TransitionOutcome.Invalid;
        Status = DisputeStatus.Open;
        ResolvedAt = null;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome ResolveForBuyer(DateTimeOffset now)
    {
        if (Status == DisputeStatus.ResolvedBuyer)
            return TransitionOutcome.NoOp;
        if (Status is not (DisputeStatus.Open or DisputeStatus.UnderReview))
            return TransitionOutcome.Invalid;
        Status = DisputeStatus.ResolvedBuyer;
        ResolvedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome ResolveForSeller(DateTimeOffset now)
    {
        if (Status == DisputeStatus.ResolvedSeller)
            return TransitionOutcome.NoOp;
        if (Status is not (DisputeStatus.Open or DisputeStatus.UnderReview))
            return TransitionOutcome.Invalid;
        Status = DisputeStatus.ResolvedSeller;
        ResolvedAt = now;
        return TransitionOutcome.Applied;
    }
}
