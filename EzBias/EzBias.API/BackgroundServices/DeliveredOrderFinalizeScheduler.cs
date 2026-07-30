using EzBias.Application.Features.Orders;

namespace EzBias.API.BackgroundServices;

public sealed class DeliveredOrderFinalizeScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveredOrderFinalizeScheduler> _logger;
    private readonly IConfiguration _config;

    public DeliveredOrderFinalizeScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<DeliveredOrderFinalizeScheduler> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds =
            _config.GetValue<int?>("Order:DeliveredFinalizeScheduler:IntervalSeconds") ?? 60;
        if (intervalSeconds < 1) intervalSeconds = 1;

        var graceDays =
            _config.GetValue<int?>("Order:DeliveredFinalizeScheduler:GraceDays") ?? 3;
        if (graceDays < 0) graceDays = 0;

        _logger.LogInformation(
            "DeliveredOrderFinalizeScheduler started. Interval={IntervalSeconds}s, GraceDays={GraceDays}",
            intervalSeconds,
            graceDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var finalizer = scope.ServiceProvider
                    .GetRequiredService<IDeliveredOrderFinalizationApplicationService>();
                var result = await finalizer.RunAsync(
                    DateTimeOffset.UtcNow,
                    graceDays,
                    stoppingToken);

                if (result.FinalizedCount > 0)
                {
                    _logger.LogInformation(
                        "Delivered order finalizer completed {Count} orders.",
                        result.FinalizedCount);
                }

                foreach (var error in result.Errors)
                    _logger.LogError("Delivered order finalization failed: {Error}", error);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeliveredOrderFinalizeScheduler tick failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}
