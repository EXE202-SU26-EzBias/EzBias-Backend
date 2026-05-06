using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IBidRepository
{
    void Add(Bid bid);
    Task<decimal?> GetHighestBidAmountAsync(long auctionId, CancellationToken ct);
    Task ClearWinningFlagsAsync(long auctionId, CancellationToken ct);
    Task<Bid?> GetTopBidAsync(long auctionId, CancellationToken ct);
}
