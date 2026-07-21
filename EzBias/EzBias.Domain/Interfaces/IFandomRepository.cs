using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IFandomRepository
{
    Task<IReadOnlyList<Fandom>> GetActiveAsync(CancellationToken ct);
    Task<Fandom?> GetByIdAsync(string id, CancellationToken ct);
    Task<Fandom?> GetByNormalizedNameAsync(string normalizedName, CancellationToken ct);
    void Add(Fandom fandom);
    void Detach(Fandom fandom);
}
