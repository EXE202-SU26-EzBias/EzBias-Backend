using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using EzBias.Domain.Exceptions;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EzBias.Infrastructure.Repositories;

/// <summary>
/// Wraps UnitOfWork to automatically push newly persisted Notification entities
/// to connected clients via SignalR after SaveChangesAsync.
/// </summary>
public sealed class NotificationDispatchingUnitOfWork : IUnitOfWork
{
    private readonly EzBiasDbContext _db;
    private readonly IRealtimeNotifier _notifier;

    public NotificationDispatchingUnitOfWork(EzBiasDbContext db, IRealtimeNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capture pending new notifications before saving (Id is 0 at this point)
        var pendingNotifications = _db.ChangeTracker
            .Entries<Notification>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        int result;
        try
        {
            result = await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsFandomWriteConflict(ex))
        {
            throw new FandomWriteConflictException("A conflicting fandom was created concurrently.", ex);
        }

        // After save, EF has assigned Ids — push realtime best-effort
        if (pendingNotifications.Count > 0)
        {
            var pushTasks = pendingNotifications
                .Select(n => _notifier.PushAsync(n, cancellationToken));

            await Task.WhenAll(pushTasks).ConfigureAwait(false);
        }

        return result;
    }

    private static bool IsFandomWriteConflict(DbUpdateException exception)
    {
        var postgresException = exception.InnerException as PostgresException;
        return postgresException?.ConstraintName is "ux_fandoms_normalized_name" or "PK_fandoms";
    }
}
