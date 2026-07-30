using EzBias.Application.Features.Notifications;

namespace EzBias.API.BackgroundServices;

public sealed class NotificationDispatchScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDispatchScheduler> _logger;
    private readonly NotificationDispatchOptions _options;

    public NotificationDispatchScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationDispatchScheduler> logger,
        NotificationDispatchOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NotificationDispatchScheduler started. Interval={IntervalSeconds}s, BatchSize={BatchSize}",
            _options.IntervalSeconds,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<INotificationDispatchProcessor>();

                var dispatched = await processor.DispatchPendingAsync(stoppingToken);
                if (dispatched > 0)
                    _logger.LogInformation("Dispatched {Count} realtime notifications.", dispatched);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification dispatch tick failed.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.IntervalSeconds),
                stoppingToken);
        }
    }
}
