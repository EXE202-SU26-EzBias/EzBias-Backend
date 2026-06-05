using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly EzBiasDbContext _db;

    public OrderRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void AddRange(IEnumerable<Order> orders) => _db.Orders.AddRange(orders);

    public void Add(Order order) => _db.Orders.Add(order);

    public Task<bool> ExistsByAuctionIdAsync(long auctionId, CancellationToken ct)
        => _db.Orders.AnyAsync(x => x.AuctionId == auctionId, ct);

    public Task<Order?> GetByAuctionIdAsync(long auctionId, CancellationToken ct)
        => _db.Orders.FirstOrDefaultAsync(x => x.AuctionId == auctionId, ct);

    public Task<Order?> GetByIdAsync(long orderId, CancellationToken ct)
        => _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, ct);

    public Task<Order?> GetByIdWithItemsAsync(long orderId, CancellationToken ct)
        => _db.Orders
            .Include(x => x.User)
            .Include(x => x.Seller)
            .Include(x => x.Items)
            .Include(x => x.PaymentOrders)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

    public async Task<IReadOnlyList<Order>> GetByBuyerAsync(long buyerId, CancellationToken ct)
        => await _db.Orders
            .Include(x => x.User)
            .Include(x => x.Seller)
            .Include(x => x.Items)
            .Include(x => x.PaymentOrders)
            .Where(x => x.UserId == buyerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetBySellerAsync(long sellerId, CancellationToken ct)
        => await _db.Orders
            .Include(x => x.User)
            .Include(x => x.Seller)
            .Include(x => x.Items)
            .Include(x => x.PaymentOrders)
            .Where(x => x.SellerId == sellerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetDeliveredOverdueWithoutOpenDisputeOrPendingRefundAsync(DateTimeOffset deliveredBefore, CancellationToken ct)
        => await _db.Orders
            .Where(x => x.Status == OrderStatus.Delivered
                && x.DeliveredAt.HasValue
                && x.DeliveredAt.Value <= deliveredBefore
                && (x.Dispute == null || x.Dispute.Status == DisputeStatus.Closed || x.Dispute.Status == DisputeStatus.ResolvedBuyer || x.Dispute.Status == DisputeStatus.ResolvedSeller)
                && !x.Refunds.Any(r => r.Status == RefundStatus.Pending))
            .ToListAsync(ct);

    public Task<bool> HasUserPurchasedProductAsync(long userId, long productId, CancellationToken ct)
        => _db.Orders.AnyAsync(o => o.UserId == userId
            && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed)
            && o.Items.Any(i => i.ProductId == productId), ct);

    public void Remove(Order order) => _db.Orders.Remove(order);
}
