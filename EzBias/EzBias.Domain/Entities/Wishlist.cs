namespace EzBias.Domain.Entities;

public class Wishlist
{
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
