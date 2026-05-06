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
}
