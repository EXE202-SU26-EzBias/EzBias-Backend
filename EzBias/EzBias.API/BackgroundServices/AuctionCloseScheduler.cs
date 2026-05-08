using EzBias.Domain.Entities;
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
                var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
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

                        var order = await orders.GetByAuctionIdAsync(auction.Id, stoppingToken);
                        if (order is null)
                        {
                            var productImage = auction.Product.Images.FirstOrDefault(x => x.SortOrder == 1)?.Url
                                ?? auction.Product.Images.FirstOrDefault()?.Url
                                ?? string.Empty;

                            order = new Order
                            {
                                UserId = topBid.UserId,
                                SellerId = auction.SellerId,
                                Source = OrderSource.Auction,
                                AuctionId = auction.Id,
                                Total = topBid.Amount,
                                Status = OrderStatus.Pending,
                                AddressSnap = "{}",
                                CreatedAt = now,
                                Items =
                                {
                                    new OrderItem
                                    {
                                        ProductId = auction.ProductId,
                                        ProductName = auction.Product.Name,
                                        ProductImage = productImage,
                                        Quantity = 1,
                                        UnitPrice = topBid.Amount,
                                        Subtotal = topBid.Amount
                                    }
                                }
                            };

                            orders.Add(order);
                        }

                        pendingPayment++;
                    }

                    auction.EndedAt = now;
                    auction.UpdatedAt = now;
                }

                var winnerExpired = await auctions.GetPendingPaymentExpiredAsync(now, stoppingToken);
                var winnerFailed = 0;
                foreach (var auction in winnerExpired)
                {
                    auction.Status = AuctionStatus.WinnerFailed;
                    auction.UpdatedAt = now;

                    var auctionOrder = await orders.GetByAuctionIdAsync(auction.Id, stoppingToken);
                    if (auctionOrder is not null && auctionOrder.Status == OrderStatus.Pending)
                    {
                        auctionOrder.Status = OrderStatus.Canceled;
                        auctionOrder.UpdatedAt = now;
                    }

                    winnerFailed++;
                }

                if (closable.Count > 0 || winnerFailed > 0)
                {
                    await uow.SaveChangesAsync(stoppingToken);

                    if (closable.Count > 0)
                    {
                        _logger.LogInformation(
                            "Auction scheduler closed {ClosedCount} auctions (NoWinner={NoWinner}, PendingPayment={PendingPayment})",
                            closable.Count,
                            noWinner,
                            pendingPayment);
                    }

                    if (winnerFailed > 0)
                    {
                        _logger.LogInformation("Auction scheduler marked {Count} winner-timeout auctions as WinnerFailed", winnerFailed);
                    }
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
