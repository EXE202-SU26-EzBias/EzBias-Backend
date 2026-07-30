using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly EzBiasDbContext _db;

    public NotificationRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(long userId, CancellationToken ct)
        => await _db.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<Notification?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Notification>> ClaimPendingDispatchAsync(
        DateTimeOffset now,
        int batchSize,
        TimeSpan leaseDuration,
        Guid leaseId,
        CancellationToken ct)
    {
        if (batchSize <= 0)
            return Array.Empty<Notification>();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var notifications = await _db.Notifications
            .FromSqlInterpolated($"""
                SELECT *
                FROM notifications
                WHERE dispatched_at IS NULL
                  AND dispatch_failed_at IS NULL
                  AND (next_dispatch_at IS NULL OR next_dispatch_at <= {now})
                  AND (dispatch_locked_until IS NULL OR dispatch_locked_until < {now})
                ORDER BY created_at, id
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        var lockedUntil = now.Add(leaseDuration);
        foreach (var notification in notifications)
        {
            notification.DispatchLeaseId = leaseId;
            notification.DispatchLockedUntil = lockedUntil;
            notification.DispatchAttempts++;
        }

        if (notifications.Count > 0)
            await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return notifications;
    }

    public Task MarkDispatchSucceededAsync(
        long notificationId,
        Guid leaseId,
        DateTimeOffset dispatchedAt,
        CancellationToken ct)
        => _db.Notifications
            .Where(x => x.Id == notificationId && x.DispatchLeaseId == leaseId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DispatchedAt, dispatchedAt)
                .SetProperty(x => x.DispatchLeaseId, (Guid?)null)
                .SetProperty(x => x.DispatchLockedUntil, (DateTimeOffset?)null)
                .SetProperty(x => x.NextDispatchAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastDispatchError, (string?)null), ct);

    public Task MarkDispatchFailedAsync(
        long notificationId,
        Guid leaseId,
        DateTimeOffset nextDispatchAt,
        string error,
        bool permanentlyFailed,
        DateTimeOffset? failedAt,
        CancellationToken ct)
    {
        var normalizedError = error.Length > 4000 ? error.Substring(0, 4000) : error;

        return _db.Notifications
            .Where(x => x.Id == notificationId && x.DispatchLeaseId == leaseId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.NextDispatchAt, nextDispatchAt)
                .SetProperty(x => x.LastDispatchError, normalizedError)
                .SetProperty(x => x.DispatchFailedAt, permanentlyFailed ? failedAt : null)
                .SetProperty(x => x.DispatchLeaseId, (Guid?)null)
                .SetProperty(x => x.DispatchLockedUntil, (DateTimeOffset?)null), ct);
    }

    public void Add(Notification notification) => _db.Notifications.Add(notification);

    public void AddRange(IEnumerable<Notification> notifications) => _db.Notifications.AddRange(notifications);
}
