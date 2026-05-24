using EzBias.Domain.Entities;

namespace EzBias.Application.Features.Notifications;

/// <summary>
/// Pushes a persisted notification to the connected client in realtime.
/// </summary>
public interface IRealtimeNotifier
{
    Task PushAsync(Notification notification, CancellationToken ct = default);
}
