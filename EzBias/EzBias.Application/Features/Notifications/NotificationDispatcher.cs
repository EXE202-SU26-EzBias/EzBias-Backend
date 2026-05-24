using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Notifications;

/// <summary>
/// Persists a notification and immediately pushes it to the connected client via realtime channel.
/// Use this instead of calling INotificationRepository.Add directly when realtime push is desired.
/// </summary>
public sealed class NotificationDispatcher
{
    private readonly INotificationRepository _repository;
    private readonly IRealtimeNotifier _realtime;

    public NotificationDispatcher(INotificationRepository repository, IRealtimeNotifier realtime)
    {
        _repository = repository;
        _realtime = realtime;
    }

    /// <summary>
    /// Adds the notification to the repository (to be saved with the current UoW)
    /// and fires a realtime push. The push is best-effort — failure does not affect persistence.
    /// </summary>
    public void Queue(Notification notification)
        => _repository.Add(notification);

    /// <summary>
    /// Pushes the notification to the connected client after it has been persisted (Id is set).
    /// Call this after SaveChangesAsync.
    /// </summary>
    public Task PushAsync(Notification notification, CancellationToken ct = default)
        => _realtime.PushAsync(notification, ct);

    /// <summary>
    /// Convenience: queue + push in one call. Use when you have a single notification to dispatch.
    /// </summary>
    public async Task DispatchAsync(Notification notification, IUnitOfWork uow, CancellationToken ct)
    {
        _repository.Add(notification);
        await uow.SaveChangesAsync(ct);
        await _realtime.PushAsync(notification, ct);
    }
}
