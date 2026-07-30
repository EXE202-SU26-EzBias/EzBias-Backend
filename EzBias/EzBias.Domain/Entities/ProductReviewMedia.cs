using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class ProductReviewMedia
{
    public long Id { get; set; }
    public long ProductReviewId { get; set; }
    public ReviewMediaType MediaType { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string StoragePublicId { get; set; } = string.Empty;
    public short SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ProductReview ProductReview { get; set; } = null!;
}
