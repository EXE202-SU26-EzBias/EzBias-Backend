using EzBias.Application.Features.Notifications.Dtos;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Notifications;

public class NotificationApplicationService : INotificationApplicationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _uow;

    public NotificationApplicationService(INotificationRepository notifications, IUnitOfWork uow)
    {
        _notifications = notifications;
        _uow = uow;
    }

    public async Task<IReadOnlyList<NotificationItem>> GetMyAsync(long userId, CancellationToken ct)
        => (await _notifications.GetByUserIdAsync(userId, ct))
            .Select(x => new NotificationItem(x.Id, x.Type, x.Title, x.Body, x.Meta, x.IsRead, x.CreatedAt, x.ReadAt))
            .ToList();

    public async Task<(bool Success, string? Error)> MarkReadAsync(long userId, long id, CancellationToken ct)
    {
        var n = await _notifications.GetByIdAsync(id, ct);
        if (n is null) return (false, "Notification not found.");
        if (n.UserId != userId) return (false, "Forbidden.");
        if (!n.IsRead)
        {
            n.IsRead = true;
            n.ReadAt = DateTimeOffset.UtcNow;
            await _uow.SaveChangesAsync(ct);
        }
        return (true, null);
    }

    public async Task<int> MarkReadAllAsync(long userId, CancellationToken ct)
    {
        var items = await _notifications.GetByUserIdAsync(userId, ct);
        var changed = 0;
        foreach (var n in items.Where(x => !x.IsRead))
        {
            n.IsRead = true;
            n.ReadAt = DateTimeOffset.UtcNow;
            changed++;
        }

        if (changed > 0) await _uow.SaveChangesAsync(ct);
        return changed;
    }
}
