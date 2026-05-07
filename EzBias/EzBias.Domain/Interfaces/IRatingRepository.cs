using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IRatingRepository
{
    void Add(Rating rating);
    Task<bool> ExistsByOrderIdAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<Rating>> GetBySellerIdAsync(long sellerId, CancellationToken ct);
}
