using EzBias.Application.Features.Orders;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.API.BackgroundServices;

public class DeliveredOrderFinalizeScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveredOrderFinalizeScheduler> _logger;
    private readonly IConfiguration _config;

    public DeliveredOrderFinalizeScheduler(IServiceScopeFactory scopeFactory, ILogger<DeliveredOrderFinalizeScheduler> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _config.GetValue<int?>("Order:DeliveredFinalizeScheduler:IntervalSeconds") ?? 60;
        if (intervalSeconds < 1) intervalSeconds = 1;

        var graceDays = _config.GetValue<int?>("Order:DeliveredFinalizeScheduler:GraceDays") ?? 3;
        if (graceDays < 0) graceDays = 0;

        _logger.LogInformation("DeliveredOrderFinalizeScheduler started. Interval={IntervalSeconds}s, GraceDays={GraceDays}", intervalSeconds, graceDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderApplicationService>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var now = DateTimeOffset.UtcNow;
                var deliveredBefore = now.AddDays(-graceDays);
                var candidates = await orders.GetDeliveredOverdueWithoutOpenDisputeOrPendingRefundAsync(deliveredBefore, stoppingToken);

                var finalizedCount = 0;
                foreach (var order in candidates)
                {
                    await using var transaction = await uow.BeginTransactionAsync(stoppingToken);

                    order.CompletedAt = now;
                    order.Status = OrderStatus.Completed;
                    order.UpdatedAt = now;

                    await orderService.FinalizeOrderPayoutAsync(order, now, stoppingToken);
                    await uow.SaveChangesAsync(stoppingToken);
                    await transaction.CommitAsync(stoppingToken);

                    finalizedCount++;
                }

                if (finalizedCount > 0)
                {
                    _logger.LogInformation("Delivered order finalizer completed {Count} orders.", finalizedCount);
                }
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
