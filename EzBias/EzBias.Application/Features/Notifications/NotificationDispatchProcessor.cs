using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Notifications;

public sealed class NotificationDispatchProcessor : INotificationDispatchProcessor
{
    private readonly INotificationRepository _notifications;
    private readonly IRealtimeNotifier _realtime;
    private readonly NotificationDispatchOptions _options;

    public NotificationDispatchProcessor(
        INotificationRepository notifications,
        IRealtimeNotifier realtime,
        NotificationDispatchOptions options)
    {
        _notifications = notifications;
        _realtime = realtime;
        _options = options;
    }

    public async Task<int> DispatchPendingAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var leaseId = Guid.NewGuid();
        var notifications = await _notifications.ClaimPendingDispatchAsync(
            now,
            _options.BatchSize,
            TimeSpan.FromSeconds(_options.LeaseSeconds),
            leaseId,
            ct);

        var dispatched = 0;
        foreach (var notification in notifications)
        {
            try
            {
                await _realtime.PushAsync(notification, ct);
                await _notifications.MarkDispatchSucceededAsync(
                    notification.Id,
                    leaseId,
                    DateTimeOffset.UtcNow,
                    ct);
                dispatched++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var attempt = Math.Max(notification.DispatchAttempts, 1);
                var backoffSeconds = Math.Min(
                    _options.MaxBackoffSeconds,
                    _options.BaseBackoffSeconds * Math.Pow(2, Math.Min(attempt - 1, 30)));
                var permanentlyFailed = attempt >= _options.MaxAttempts;
                DateTimeOffset? failureAt = permanentlyFailed ? DateTimeOffset.UtcNow : null;

                await _notifications.MarkDispatchFailedAsync(
                    notification.Id,
                    leaseId,
                    DateTimeOffset.UtcNow.AddSeconds(backoffSeconds),
                    ex.Message,
                    permanentlyFailed,
                    failureAt,
                    ct);
            }
        }

        return dispatched;
    }
}
