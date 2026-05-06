using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface ICartRepository
{
    Task<List<CartItem>> GetByUserIdAsync(long userId, CancellationToken ct);
    Task<List<CartItem>> GetByUserIdAndIdsAsync(long userId, IReadOnlyList<long> cartItemIds, CancellationToken ct);
    Task<CartItem?> GetByUserAndProductAsync(long userId, long productId, CancellationToken ct);
    Task<CartItem?> GetByIdAsync(long cartItemId, CancellationToken ct);
    void Add(CartItem item);
    void Remove(CartItem item);
    void RemoveRange(IEnumerable<CartItem> items);
}
