namespace EzBias.Domain.Entities;

public class ProductReview
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long UserId { get; set; }
    public short Stars { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
