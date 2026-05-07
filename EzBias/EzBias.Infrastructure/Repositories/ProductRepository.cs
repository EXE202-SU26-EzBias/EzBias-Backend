using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly EzBiasDbContext _db;

    public ProductRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<Product?> GetByIdAsync(long productId, CancellationToken ct)
        => _db.Products.FirstOrDefaultAsync(x => x.Id == productId, ct);

    public async Task<IReadOnlyList<Product>> GetActiveAsync(string? fandomId, CancellationToken ct)
    {
        var query = _db.Products.Where(x => x.Status == Domain.Enums.ProductStatus.Active && x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(fandomId)) query = query.Where(x => x.FandomId == fandomId);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }
}
