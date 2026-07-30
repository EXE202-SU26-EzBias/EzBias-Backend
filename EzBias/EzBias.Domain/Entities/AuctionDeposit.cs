using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class AuctionDeposit
{
    public long Id { get; set; }
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public DepositState State { get; set; } = DepositState.PendingPayment;

    public long? PaymentId { get; set; }   // linked deposit Payment
    public long? RefundId { get; set; }    // linked Refund (when Refunded)

    public DateTimeOffset? HeldAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset? ForfeitedAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }

    public string? LastError { get; set; }                               // Req 3.6, 7.6
    public int ForfeitRetryCount { get; set; } = 0;                      // Req 7.6

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Auction Auction { get; set; } = null!;
    public User User { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Refund? Refund { get; set; }

    /// <summary>
    /// Applies one of the legal deposit lifecycle transitions.
    /// </summary>
    public TransitionOutcome TryTransitionTo(DepositState target, DateTimeOffset now)
    {
        if (State == target)
            return TransitionOutcome.NoOp;

        if (State is DepositState.Applied
            or DepositState.Forfeited
            or DepositState.Refunded
            or DepositState.Failed)
            return TransitionOutcome.Terminal;

        var legal = State switch
        {
            DepositState.PendingPayment => target is DepositState.Held or DepositState.Failed,
            DepositState.Held => target is DepositState.Refunded or DepositState.Applied or DepositState.Forfeited,
            _ => false
        };

        if (!legal)
            return TransitionOutcome.Invalid;

        State = target;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }
}
