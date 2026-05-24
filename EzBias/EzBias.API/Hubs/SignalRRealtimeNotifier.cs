using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task PushAsync(Notification notification, CancellationToken ct = default)
        => _hub
            .Clients
            .Group(NotificationHub.UserGroup(notification.UserId))
            .SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                type = notification.Type.ToString(),
                title = notification.Title,
                body = notification.Body,
                meta = notification.Meta,
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt
            }, ct);
}
