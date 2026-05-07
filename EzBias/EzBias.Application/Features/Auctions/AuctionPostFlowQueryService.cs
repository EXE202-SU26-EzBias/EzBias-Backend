using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Auctions;

public class AuctionPostFlowQueryService : IAuctionPostFlowQueryService
{
    private readonly IAuctionRepository _auctions;

    public AuctionPostFlowQueryService(IAuctionRepository auctions)
    {
        _auctions = auctions;
    }

    public async Task<IReadOnlyList<BuyerAuctionPostItem>> GetBuyerWonAsync(long buyerId, bool onlyPendingPayment, CancellationToken ct)
    {
        var items = await _auctions.GetWonByBuyerAsync(buyerId, onlyPendingPayment, ct);
        return items.Select(x => new BuyerAuctionPostItem(x.Id, x.ProductId, x.FinalPrice ?? 0m, x.Status, x.WinnerPaymentDeadline, x.EndedAt)).ToList();
    }

    public async Task<IReadOnlyList<SellerAuctionEndedItem>> GetSellerEndedAsync(long sellerId, CancellationToken ct)
    {
        var items = await _auctions.GetEndedBySellerAsync(sellerId, ct);
        return items.Select(x => new SellerAuctionEndedItem(x.Id, x.ProductId, x.WinnerId, x.FinalPrice, x.Status, x.EndedAt, x.WinnerPaymentDeadline)).ToList();
    }
}
