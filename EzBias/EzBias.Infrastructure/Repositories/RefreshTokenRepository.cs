using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly EzBiasDbContext _db;

    public RefreshTokenRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
        => _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public Task<RefreshToken?> GetByHashWithUserAsync(string tokenHash, CancellationToken ct)
        => _db.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public void Add(RefreshToken refreshToken) => _db.RefreshTokens.Add(refreshToken);
}
