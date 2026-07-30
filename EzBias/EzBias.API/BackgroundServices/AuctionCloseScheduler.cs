using EzBias.Application.Features.Auctions;

namespace EzBias.API.BackgroundServices;

public sealed class AuctionCloseScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionCloseScheduler> _logger;
    private readonly IConfiguration _config;

    public AuctionCloseScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionCloseScheduler> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds =
            _config.GetValue<int?>("Auction:CloseScheduler:IntervalSeconds") ?? 10;
        if (intervalSeconds < 1) intervalSeconds = 1;

        _logger.LogInformation(
            "AuctionCloseScheduler started. Interval={IntervalSeconds}s",
            intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var lifecycle = scope.ServiceProvider
                    .GetRequiredService<IAuctionLifecycleApplicationService>();
                var result = await lifecycle.RunAsync(
                    DateTimeOffset.UtcNow,
                    stoppingToken);

                if (result.RemindersSent > 0
                    || result.EndedNoWinner > 0
                    || result.PendingPayment > 0
                    || result.WinnerFailed > 0)
                {
                    _logger.LogInformation(
                        "Auction lifecycle completed. Reminders={Reminders}, NoWinner={NoWinner}, PendingPayment={PendingPayment}, WinnerFailed={WinnerFailed}",
                        result.RemindersSent,
                        result.EndedNoWinner,
                        result.PendingPayment,
                        result.WinnerFailed);
                }

                foreach (var error in result.Errors)
                    _logger.LogError("Auction lifecycle record failed: {Error}", error);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuctionCloseScheduler tick failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}
