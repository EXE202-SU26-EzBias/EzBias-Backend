using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Auction
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long SellerId { get; set; }

    public decimal FloorPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal CurrentBid { get; set; } = 0m;
    public decimal RequiredDepositAmount { get; set; }
    public bool IsUrgent { get; set; } = false;
    public bool HasProofImage { get; set; } = false;

    public int ExtensionSeconds { get; set; } = 300;
    public int TriggerBeforeEnd { get; set; } = 60;
    public int ExtensionCount { get; set; } = 0;

    public AuctionStatus Status { get; set; } = AuctionStatus.Live;
    public long? WinnerId { get; set; }
    public decimal? FinalPrice { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset? WinnerPaymentDeadline { get; set; }
    public bool ReminderSent5Min { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    // Track if this auction has been relisted to a new auction
    public long? RelistedToAuctionId { get; set; }

    public Product Product { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public User? Winner { get; set; }
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<AuctionDeposit> Deposits { get; set; } = new List<AuctionDeposit>();

    public TransitionOutcome Publish(DateTimeOffset now)
    {
        if (Status == AuctionStatus.Live)
            return TransitionOutcome.NoOp;
        if (Status is AuctionStatus.Canceled
            or AuctionStatus.Sold
            or AuctionStatus.EndedNoWinner
            or AuctionStatus.EndedPendingPayment)
            return TransitionOutcome.Invalid;

        Status = AuctionStatus.Live;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome Cancel(DateTimeOffset now)
    {
        if (Status == AuctionStatus.Canceled)
            return TransitionOutcome.NoOp;
        if (Status is AuctionStatus.Sold
            or AuctionStatus.EndedNoWinner
            or AuctionStatus.EndedPendingPayment
            or AuctionStatus.WinnerFailed)
            return TransitionOutcome.Invalid;

        Status = AuctionStatus.Canceled;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkEndedNoWinner(DateTimeOffset now)
    {
        if (Status == AuctionStatus.EndedNoWinner)
            return TransitionOutcome.NoOp;
        if (Status is not (AuctionStatus.Live or AuctionStatus.Extended))
            return TransitionOutcome.Invalid;

        Status = AuctionStatus.EndedNoWinner;
        EndedAt = now;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome AssignWinner(long winnerId, decimal finalPrice, DateTimeOffset paymentDeadline, DateTimeOffset now)
    {
        if (Status == AuctionStatus.EndedPendingPayment
            && WinnerId == winnerId
            && FinalPrice == finalPrice)
            return TransitionOutcome.NoOp;
        if (Status is not (AuctionStatus.Live or AuctionStatus.Extended))
            return TransitionOutcome.Invalid;

        Status = AuctionStatus.EndedPendingPayment;
        WinnerId = winnerId;
        FinalPrice = finalPrice;
        WinnerPaymentDeadline = paymentDeadline;
        EndedAt = now;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkWinnerFailed(DateTimeOffset now)
    {
        if (Status == AuctionStatus.WinnerFailed)
            return TransitionOutcome.NoOp;
        if (Status != AuctionStatus.EndedPendingPayment)
            return TransitionOutcome.Invalid;

        Status = AuctionStatus.WinnerFailed;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkSold(DateTimeOffset now)
    {
        if (Status == AuctionStatus.Sold)
            return TransitionOutcome.NoOp;
        if (Status != AuctionStatus.EndedPendingPayment)
            return TransitionOutcome.Invalid;

        Status = AuctionStatus.Sold;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome RecordBid(decimal amount, DateTimeOffset now)
    {
        if (Status is not (AuctionStatus.Live or AuctionStatus.Extended))
            return TransitionOutcome.Invalid;

        CurrentBid = amount;
        Status = AuctionStatus.Live;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }
}
