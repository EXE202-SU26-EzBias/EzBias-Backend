using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(long userId, CancellationToken ct);
    Task<Notification?> GetByIdAsync(long id, CancellationToken ct);
    void Add(Notification notification);
    void AddRange(IEnumerable<Notification> notifications);
}
