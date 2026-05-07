using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(long userId, CancellationToken ct);
    Task<Notification?> GetByIdAsync(long id, CancellationToken ct);
}
