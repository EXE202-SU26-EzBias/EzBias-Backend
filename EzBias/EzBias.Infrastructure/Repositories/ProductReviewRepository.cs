using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class ProductReviewRepository : IProductReviewRepository
{
    private readonly EzBiasDbContext _db;

    public ProductReviewRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(ProductReview review) => _db.ProductReviews.Add(review);

    public void Remove(ProductReview review) => _db.ProductReviews.Remove(review);

    public Task<ProductReview?> GetByIdAsync(long id, CancellationToken ct)
        => _db.ProductReviews.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ProductReview?> GetByProductAndUserAsync(long productId, long userId, CancellationToken ct)
        => _db.ProductReviews
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId, ct);

    public async Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(long productId, CancellationToken ct)
        => await _db.ProductReviews
            .Include(x => x.User)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductReview>> GetAllAsync(CancellationToken ct)
        => await _db.ProductReviews
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<(decimal AvgStars, int TotalReviews)> GetSellerStatsAsync(long sellerId, CancellationToken ct)
    {
        // Join ProductReviews → Products to filter by seller
        var stats = await _db.ProductReviews
            .Where(r => r.Product.SellerId == sellerId)
            .GroupBy(_ => 1)
            .Select(g => new { Avg = g.Average(r => (double)r.Stars), Count = g.Count() })
            .FirstOrDefaultAsync(ct);

        if (stats is null) return (0m, 0);
        return (Math.Round((decimal)stats.Avg, 2), stats.Count);
    }
}
