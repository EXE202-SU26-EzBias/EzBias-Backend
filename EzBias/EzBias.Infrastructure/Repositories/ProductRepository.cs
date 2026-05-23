using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
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
        => _db.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == productId, ct);

    public async Task<IReadOnlyList<Product>> GetBySellerAsync(long sellerId, CancellationToken ct)
        => await _db.Products
            .Include(x => x.Images)
            .Where(x => x.SellerId == sellerId && x.DeletedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetActiveAsync(string? fandomId, CancellationToken ct)
    {
        var query = _db.Products
            .Include(x => x.Images)
            .Where(x => x.Status == ProductStatus.Active && x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(fandomId)) query = query.Where(x => x.FandomId == fandomId);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public void Add(Product product) => _db.Products.Add(product);
}
