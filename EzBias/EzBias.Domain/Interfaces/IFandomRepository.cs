using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IFandomRepository
{
    Task<IReadOnlyList<Fandom>> GetActiveAsync(CancellationToken ct);
}
