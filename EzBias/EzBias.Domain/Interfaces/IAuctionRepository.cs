using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Domain.Interfaces;

public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(long auctionId, CancellationToken ct);
    Task<Auction?> GetByIdWithRelationsAsync(long auctionId, CancellationToken ct);
    Task<Auction?> GetByIdWithProductAsync(long auctionId, CancellationToken ct);
    Task<bool> ExistsLiveByProductIdAsync(long productId, CancellationToken ct);
    Task<bool> ExistsDraftOrLiveByProductIdAsync(long productId, CancellationToken ct);
    Task<bool> HasAnyBidAsync(long auctionId, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetBySellerAsync(long sellerId, AuctionStatus? status, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetPublicAsync(AuctionStatus? status, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetClosableAsync(DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetPendingPaymentExpiredAsync(DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetNearEndAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<Auction?> GetByOrderIdAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetWonByBuyerAsync(long buyerId, bool onlyPendingPayment, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetEndedBySellerAsync(long sellerId, CancellationToken ct);
    void Add(Auction auction);
}
