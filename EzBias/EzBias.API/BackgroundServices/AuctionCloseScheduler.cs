using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.API.BackgroundServices;

public class AuctionCloseScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionCloseScheduler> _logger;
    private readonly IConfiguration _config;

    public AuctionCloseScheduler(IServiceScopeFactory scopeFactory, ILogger<AuctionCloseScheduler> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _config.GetValue<int?>("Auction:CloseScheduler:IntervalSeconds") ?? 10;
        if (intervalSeconds < 1) intervalSeconds = 1;

        _logger.LogInformation("AuctionCloseScheduler started. Interval={IntervalSeconds}s", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var auctions = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                var bids = scope.ServiceProvider.GetRequiredService<IBidRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var now = DateTimeOffset.UtcNow;
                var closable = await auctions.GetClosableAsync(now, stoppingToken);

                var noWinner = 0;
                var pendingPayment = 0;

                foreach (var auction in closable)
                {
                    var topBid = await bids.GetTopBidAsync(auction.Id, stoppingToken);
                    if (topBid is null || (auction.ReservePrice.HasValue && topBid.Amount < auction.ReservePrice.Value))
                    {
                        auction.Status = AuctionStatus.EndedNoWinner;
                        noWinner++;
                    }
                    else
                    {
                        auction.Status = AuctionStatus.EndedPendingPayment;
                        auction.WinnerId = topBid.UserId;
                        auction.FinalPrice = topBid.Amount;
                        auction.WinnerPaymentDeadline = now.AddHours(24);
                        pendingPayment++;
                    }

                    auction.EndedAt = now;
                    auction.UpdatedAt = now;
                }

                if (closable.Count > 0)
                {
                    await uow.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation(
                        "Auction scheduler closed {ClosedCount} auctions (NoWinner={NoWinner}, PendingPayment={PendingPayment})",
                        closable.Count,
                        noWinner,
                        pendingPayment);
                }
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
