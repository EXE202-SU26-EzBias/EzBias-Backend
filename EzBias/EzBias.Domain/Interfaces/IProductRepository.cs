using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(long productId, CancellationToken ct);
    Task<IReadOnlyList<Product>> GetActiveAsync(string? fandomId, CancellationToken ct);
}
