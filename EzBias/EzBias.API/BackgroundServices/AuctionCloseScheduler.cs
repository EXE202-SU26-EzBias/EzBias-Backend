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
                var payments = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
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
                        //auction.WinnerPaymentDeadline = now.AddHours(24);
                        auction.WinnerPaymentDeadline = now.AddMinutes(1);

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
                                ShippingFee = 0m,
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

                        var hasPayment = order.Id > 0 && await payments.ExistsByOrderIdAsync(order.Id, stoppingToken);
                        if (!hasPayment)
                        {
                            var payment = new Payment
                            {
                                UserId = topBid.UserId,
                                Type = PaymentType.Order,
                                Amount = topBid.Amount,
                                Currency = "VND",
                                Status = PaymentStatus.Pending,
                                Reference = $"AUC-{auction.Id}-{now:yyyyMMddHHmmss}",
                                Payload = $"{{\"auctionId\":{auction.Id},\"orderSource\":\"auction\",\"deadline\":\"{auction.WinnerPaymentDeadline:O}\"}}",
                                CreatedAt = now,
                                PaymentOrders =
                                {
                                    new PaymentOrder { Order = order }
                                }
                            };

                            payments.Add(payment);
                        }

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
