using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class FandomRepository : IFandomRepository
{
    private readonly EzBiasDbContext _db;

    public FandomRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Fandom>> GetActiveAsync(CancellationToken ct)
        => await _db.Fandoms.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
}
