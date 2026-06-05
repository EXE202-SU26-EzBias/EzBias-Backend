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
}
