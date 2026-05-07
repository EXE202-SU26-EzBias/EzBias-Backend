using EzBias.Application.Features.Auctions.Dtos;

namespace EzBias.Application.Features.Auctions;

public interface IAuctionPostFlowQueryService
{
    Task<IReadOnlyList<BuyerAuctionPostItem>> GetBuyerWonAsync(long buyerId, bool onlyPendingPayment, CancellationToken ct);
    Task<IReadOnlyList<SellerAuctionEndedItem>> GetSellerEndedAsync(long sellerId, CancellationToken ct);
}
