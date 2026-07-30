using EzBias.Domain.Entities;

namespace EzBias.Application.Features.Notifications;

public interface IRealtimeNotifier
{
    Task PushAsync(Notification notification, CancellationToken ct = default);
}
