using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Auctions;

public class AuctionClosingApplicationService : IAuctionClosingApplicationService
{
    private readonly IAuctionRepository _auctions;
    private readonly IBidRepository _bids;
    private readonly IUnitOfWork _uow;

    public AuctionClosingApplicationService(IAuctionRepository auctions, IBidRepository bids, IUnitOfWork uow)
    {
        _auctions = auctions;
        _bids = bids;
        _uow = uow;
    }

    public async Task<CloseAuctionsResponse> CloseExpiredAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var closable = await _auctions.GetClosableAsync(now, ct);

        var noWinner = 0;
        var pendingPayment = 0;

        foreach (var auction in closable)
        {
            var topBid = await _bids.GetTopBidAsync(auction.Id, ct);
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
                pendingPayment++;
            }

            auction.EndedAt = now;
            auction.UpdatedAt = now;
        }

        await _uow.SaveChangesAsync(ct);

        return new CloseAuctionsResponse(closable.Count, noWinner, pendingPayment);
    }
}
