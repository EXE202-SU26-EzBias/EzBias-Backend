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

    public Product Product { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public User? Winner { get; set; }
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
