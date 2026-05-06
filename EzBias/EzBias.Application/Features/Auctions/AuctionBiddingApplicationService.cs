using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Auctions;

public class AuctionBiddingApplicationService : IAuctionBiddingApplicationService
{
    private readonly IAuctionRepository _auctions;
    private readonly IBidRepository _bids;
    private readonly IUnitOfWork _uow;

    public AuctionBiddingApplicationService(IAuctionRepository auctions, IBidRepository bids, IUnitOfWork uow)
    {
        _auctions = auctions;
        _bids = bids;
        _uow = uow;
    }

    public async Task<IReadOnlyList<AuctionListItem>> GetPublicAuctionsAsync(AuctionStatus? status, CancellationToken ct)
    {
        var items = await _auctions.GetPublicAsync(status, ct);
        return items.Select(x => new AuctionListItem(x.Id, x.ProductId, x.SellerId, x.FloorPrice, x.CurrentBid, x.Status, x.EndsAt)).ToList();
    }

    public async Task<(bool Success, string? Error, AuctionDetailItem? Data)> GetDetailAsync(long auctionId, CancellationToken ct)
    {
        var item = await _auctions.GetByIdAsync(auctionId, ct);
        if (item is null) return (false, "Auction not found.", null);

        return (true, null, new AuctionDetailItem(item.Id, item.ProductId, item.SellerId, item.FloorPrice, item.ReservePrice, item.CurrentBid, item.Status, item.EndsAt, item.ExtensionSeconds, item.TriggerBeforeEnd));
    }

    public async Task<(bool Success, string? Error, PlaceBidResponse? Data)> PlaceBidAsync(long bidderId, long auctionId, PlaceBidRequest request, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdWithProductAsync(auctionId, ct);
        if (auction is null) return (false, "Auction not found.", null);
        if (auction.Status is not (AuctionStatus.Live or AuctionStatus.Extended)) return (false, "Auction is not live.", null);
        if (auction.EndsAt <= DateTimeOffset.UtcNow) return (false, "Auction has ended.", null);
        if (auction.SellerId == bidderId) return (false, "Seller cannot bid own auction.", null);

        var highest = await _bids.GetHighestBidAmountAsync(auctionId, ct);
        var baseBid = highest ?? auction.CurrentBid;
        var minRequired = baseBid + 1000m;

        if (request.Amount < minRequired)
            return (false, $"Bid must be >= {minRequired}.", null);

        var bid = new Bid
        {
            AuctionId = auction.Id,
            UserId = bidderId,
            Amount = request.Amount,
            IsWinning = true,
            PlacedAt = DateTimeOffset.UtcNow
        };

        _bids.Add(bid);

        auction.CurrentBid = request.Amount;
        auction.Status = auction.Status == AuctionStatus.Extended ? AuctionStatus.Extended : AuctionStatus.Live;
        auction.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        return (true, null, new PlaceBidResponse(auction.Id, bid.Id, bid.Amount, auction.CurrentBid, auction.Status));
    }
}
