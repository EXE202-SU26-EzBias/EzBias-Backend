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

    public Task<Fandom?> GetByIdAsync(string id, CancellationToken ct)
        => _db.Fandoms.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Fandom?> GetByNormalizedNameAsync(string normalizedName, CancellationToken ct)
        => _db.Fandoms.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, ct);

    public void Add(Fandom fandom) => _db.Fandoms.Add(fandom);

    public void Detach(Fandom fandom) => _db.Entry(fandom).State = EntityState.Detached;
}
