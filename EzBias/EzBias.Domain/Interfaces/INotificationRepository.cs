using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(long userId, CancellationToken ct);
    Task<Notification?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<Notification>> ClaimPendingDispatchAsync(
        DateTimeOffset now,
        int batchSize,
        TimeSpan leaseDuration,
        Guid leaseId,
        CancellationToken ct);
    Task MarkDispatchSucceededAsync(long notificationId, Guid leaseId, DateTimeOffset dispatchedAt, CancellationToken ct);
    Task MarkDispatchFailedAsync(
        long notificationId,
        Guid leaseId,
        DateTimeOffset nextDispatchAt,
        string error,
        bool permanentlyFailed,
        DateTimeOffset? failedAt,
        CancellationToken ct);
    void Add(Notification notification);
    void AddRange(IEnumerable<Notification> notifications);
}
