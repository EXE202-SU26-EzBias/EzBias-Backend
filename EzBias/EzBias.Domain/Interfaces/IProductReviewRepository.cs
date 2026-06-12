using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IProductReviewRepository
{
    void Add(ProductReview review);
    void Remove(ProductReview review);
    Task<ProductReview?> GetByIdAsync(long id, CancellationToken ct);
    Task<ProductReview?> GetByProductAndUserAsync(long productId, long userId, CancellationToken ct);
    Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(long productId, CancellationToken ct);
    Task<IReadOnlyList<ProductReview>> GetAllAsync(CancellationToken ct);
    Task<(decimal AvgStars, int TotalReviews)> GetSellerStatsAsync(long sellerId, CancellationToken ct);
}
