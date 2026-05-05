namespace EzBias.Domain.Entities;

public class Bid
{
    public long Id { get; set; }
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public bool IsWinning { get; set; } = false;
    public string UsernameSnap { get; set; } = string.Empty;
    public string AvatarSnap { get; set; } = string.Empty;
    public string AvatarBgSnap { get; set; } = string.Empty;
    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;

    public Auction Auction { get; set; } = null!;
    public User User { get; set; } = null!;
}
