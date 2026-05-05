namespace EzBias.Domain.Entities;

public class CartItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
