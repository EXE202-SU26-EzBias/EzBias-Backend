namespace EzBias.Domain.Entities;

public class Rating
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long BuyerId { get; set; }
    public long SellerId { get; set; }
    public short ProductRating { get; set; }
    public short SellerRating { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Order Order { get; set; } = null!;
    public User Buyer { get; set; } = null!;
    public User Seller { get; set; } = null!;
}
