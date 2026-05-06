using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly EzBiasDbContext _db;

    public CartRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<List<CartItem>> GetByUserIdAsync(long userId, CancellationToken ct)
        => _db.CartItems
            .Include(x => x.Product)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<List<CartItem>> GetByUserIdAndIdsAsync(long userId, IReadOnlyList<long> cartItemIds, CancellationToken ct)
        => _db.CartItems
            .Include(x => x.Product)
            .Where(x => x.UserId == userId && cartItemIds.Contains(x.Id))
            .ToListAsync(ct);

    public Task<CartItem?> GetByUserAndProductAsync(long userId, long productId, CancellationToken ct)
        => _db.CartItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, ct);

    public Task<CartItem?> GetByIdAsync(long cartItemId, CancellationToken ct)
        => _db.CartItems.FirstOrDefaultAsync(x => x.Id == cartItemId, ct);

    public void Add(CartItem item) => _db.CartItems.Add(item);

    public void Remove(CartItem item) => _db.CartItems.Remove(item);

    public void RemoveRange(IEnumerable<CartItem> items) => _db.CartItems.RemoveRange(items);
}
