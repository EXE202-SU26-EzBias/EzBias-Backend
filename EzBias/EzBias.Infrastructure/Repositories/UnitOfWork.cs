using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;

namespace EzBias.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly EzBiasDbContext _db;

    public UnitOfWork(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
