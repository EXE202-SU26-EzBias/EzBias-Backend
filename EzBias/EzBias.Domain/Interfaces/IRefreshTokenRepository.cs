using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task<RefreshToken?> GetByHashWithUserAsync(string tokenHash, CancellationToken ct);
    void Add(RefreshToken refreshToken);
}
