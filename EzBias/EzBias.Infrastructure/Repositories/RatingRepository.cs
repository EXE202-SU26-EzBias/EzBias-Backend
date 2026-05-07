using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly EzBiasDbContext _db;

    public RatingRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(Rating rating) => _db.Ratings.Add(rating);

    public Task<bool> ExistsByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.Ratings.AnyAsync(x => x.OrderId == orderId, ct);

    public async Task<IReadOnlyList<Rating>> GetBySellerIdAsync(long sellerId, CancellationToken ct)
        => await _db.Ratings
            .Where(x => x.SellerId == sellerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}
