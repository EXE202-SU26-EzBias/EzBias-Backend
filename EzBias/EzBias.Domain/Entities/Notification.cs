using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Notification
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Meta { get; set; } = "{}";
    public bool IsRead { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }

    // Durable realtime-delivery state. A notification is persisted together with the
    // business transaction and dispatched asynchronously after commit.
    public DateTimeOffset? DispatchedAt { get; set; }
    public int DispatchAttempts { get; set; } = 0;
    public DateTimeOffset? NextDispatchAt { get; set; }
    public Guid? DispatchLeaseId { get; set; }
    public DateTimeOffset? DispatchLockedUntil { get; set; }
    public string? LastDispatchError { get; set; }
    public DateTimeOffset? DispatchFailedAt { get; set; }

    public User User { get; set; } = null!;
}
