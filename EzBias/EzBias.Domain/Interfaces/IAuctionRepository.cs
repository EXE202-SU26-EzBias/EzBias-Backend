using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Domain.Interfaces;

public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(long auctionId, CancellationToken ct);
    Task<Auction?> GetByIdWithProductAsync(long auctionId, CancellationToken ct);
    Task<bool> ExistsLiveByProductIdAsync(long productId, CancellationToken ct);
    Task<bool> ExistsDraftOrLiveByProductIdAsync(long productId, CancellationToken ct);
    Task<bool> HasAnyBidAsync(long auctionId, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetBySellerAsync(long sellerId, AuctionStatus? status, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetPublicAsync(AuctionStatus? status, CancellationToken ct);
    Task<IReadOnlyList<Auction>> GetClosableAsync(DateTimeOffset now, CancellationToken ct);
    void Add(Auction auction);
}
