using EzBias.Application.Common.Results;
using EzBias.Application.Features.Notifications.Dtos;

namespace EzBias.Application.Features.Notifications;

public interface INotificationApplicationService
{
    Task<IReadOnlyList<NotificationItem>> GetMyAsync(long userId, CancellationToken ct);
    Task<Result> MarkReadAsync(long userId, long id, CancellationToken ct);
    Task<int> MarkReadAllAsync(long userId, CancellationToken ct);
}
