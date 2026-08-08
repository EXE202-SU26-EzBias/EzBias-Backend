using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IOrderRepository
{
    void AddRange(IEnumerable<Order> orders);
    void Add(Order order);
    Task<Order?> GetByAuctionIdAsync(long auctionId, CancellationToken ct);
    Task<Order?> GetByIdAsync(long orderId, CancellationToken ct);
    Task<Order?> GetByIdForUpdateAsync(long orderId, CancellationToken ct);
    Task<Order?> GetByIdWithItemsAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetByBuyerAsync(long buyerId, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetBySellerAsync(long sellerId, CancellationToken ct);
    Task<bool> HasUserPurchasedProductAsync(long userId, long productId, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetDeliveredOverdueWithoutOpenDisputeOrPendingRefundAsync(DateTimeOffset deliveredBefore, CancellationToken ct);
    void Remove(Order order);
}
