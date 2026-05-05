namespace EzBias.Domain.Entities;

public class SellerFollow
{
    public long FollowerId { get; set; }
    public long SellerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Follower { get; set; } = null!;
    public User Seller { get; set; } = null!;
}
