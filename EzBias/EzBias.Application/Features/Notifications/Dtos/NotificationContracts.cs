using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Notifications.Dtos;

public record NotificationItem(long Id, NotificationType Type, string Title, string Body, string Meta, bool IsRead, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);
