namespace EzBias.Application.Features.Notifications;

public interface INotificationDispatchProcessor
{
    Task<int> DispatchPendingAsync(CancellationToken ct);
}
