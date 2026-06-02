using EzBias.Application.Features.Deposits;
using EzBias.Application.Features.Notifications;
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
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var notificationFactory = scope.ServiceProvider.GetRequiredService<INotificationFactory>();
                var deposits = scope.ServiceProvider.GetRequiredService<IDepositApplicationService>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var now = DateTimeOffset.UtcNow;
                var closable = await auctions.GetClosableAsync(now, stoppingToken);

                // --- Near-end reminder (5 minutes before end) ---
                var remind5 = await auctions.GetNearEndAsync(
                    now.AddMinutes(4), now.AddMinutes(5), stoppingToken);

                foreach (var auction in remind5)
                {
                    var bidderIds = auction.Bids.Select(b => b.UserId).Distinct();
                    foreach (var bidderId in bidderIds)
                        notifications.Add(notificationFactory.AuctionEndingSoon(bidderId, auction.Id, auction.Product.Name, 5));
                    notifications.Add(notificationFactory.AuctionEndingSoon(auction.SellerId, auction.Id, auction.Product.Name, 5));
                    auction.ReminderSent5Min = true;
                    auction.UpdatedAt = now;
                }

                if (remind5.Count > 0)
                {
                    await uow.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Auction scheduler sent {Count} near-end reminders", remind5.Count);
                }

                var noWinner = 0;
                var pendingPayment = 0;

                // Deposit processing collections (Req 5, 6, 7) — populated during the loops below,
                // processed after status changes are persisted.
                var noWinnerAuctionIds = new List<long>();
                var winnerAssignedAuctions = new List<(long AuctionId, long WinnerId)>();
                var winnerFailedAuctions = new List<(long AuctionId, long WinnerId)>();

                foreach (var auction in closable)
                {
                    var topBid = await bids.GetTopBidAsync(auction.Id, stoppingToken);
                    if (topBid is null || (auction.ReservePrice.HasValue && topBid.Amount < auction.ReservePrice.Value))
                    {
                        auction.Status = AuctionStatus.EndedNoWinner;
                        // Free up the product since auction ended with no winner
                        auction.Product.IsAuction = false;
                        auction.Product.UpdatedAt = now;
                        notifications.Add(notificationFactory.AuctionExpired(
                            auction.SellerId, auction.Id, auction.Product.Name));
                        noWinnerAuctionIds.Add(auction.Id);
                        noWinner++;
                    }
                    else
                    {
                        auction.Status = AuctionStatus.EndedPendingPayment;
                        auction.WinnerId = topBid.UserId;
                        auction.FinalPrice = topBid.Amount;
                        auction.WinnerPaymentDeadline = now.AddHours(24);

                        notifications.Add(notificationFactory.AuctionWon(
                            topBid.UserId, auction.Id, auction.Product.Name, topBid.Amount));
                        winnerAssignedAuctions.Add((auction.Id, topBid.UserId));

                        var order = await orders.GetByAuctionIdAsync(auction.Id, stoppingToken);
                        if (order is null)
                        {
                            var productImage = auction.Product.Images
                                .OrderBy(x => x.SortOrder)
                                .FirstOrDefault()?.Url
                                ?? auction.Product.PrimaryImageUrl
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
                    // Free up the product since winner failed to pay
                    auction.Product.IsAuction = false;
                    auction.Product.UpdatedAt = now;
                    auction.UpdatedAt = now;

                    if (auction.WinnerId.HasValue)
                        winnerFailedAuctions.Add((auction.Id, auction.WinnerId.Value));

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

                // Deposit lifecycle hooks (Req 5, 6, 7) — run after status changes are persisted so the
                // deposit service sees committed auction state and Held deposits.
                foreach (var (auctionId, winnerId) in winnerAssignedAuctions)
                    await deposits.RefundNonWinnerDepositsAsync(auctionId, winnerId, stoppingToken); // Req 5.1, keep winner Held (6.1)
                foreach (var auctionId in noWinnerAuctionIds)
                    await deposits.RefundNonWinnerDepositsAsync(auctionId, null, stoppingToken); // Req 5.4 refund all held
                foreach (var (auctionId, winnerId) in winnerFailedAuctions)
                    await deposits.ForfeitWinnerDepositAsync(auctionId, winnerId, stoppingToken); // Req 7.1
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
