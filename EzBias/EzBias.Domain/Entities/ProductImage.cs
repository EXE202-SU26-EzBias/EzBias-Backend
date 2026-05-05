namespace EzBias.Domain.Entities;

public class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public short SortOrder { get; set; } = 0;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product Product { get; set; } = null!;
}
