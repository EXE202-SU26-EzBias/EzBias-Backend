using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Auctions;

public class AuctionBiddingApplicationService : IAuctionBiddingApplicationService
{
    private readonly IAuctionRepository _auctions;
    private readonly IBidRepository _bids;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;

    public AuctionBiddingApplicationService(
        IAuctionRepository auctions,
        IBidRepository bids,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow)
    {
        _auctions = auctions;
        _bids = bids;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
    }

    public async Task<IReadOnlyList<AuctionListItem>> GetPublicAuctionsAsync(AuctionStatus? status, CancellationToken ct)
    {
        var items = await _auctions.GetPublicAsync(status, ct);
        return items.Select(x => new AuctionListItem(
            x.Id,
            x.ProductId,
            x.SellerId,
            x.FloorPrice,
            x.CurrentBid,
            x.Status,
            x.EndsAt,
            new AuctionSellerSummary(x.Seller.Id, x.Seller.Username, x.Seller.FullName, x.Seller.AvatarUrl, x.Seller.AvgSellerRating, x.Seller.TotalRatings),
            new AuctionProductSummary(x.Product.Id, x.Product.Name, x.Product.Artist, x.Product.Type, x.Product.Price, x.Product.Stock, x.Product.PrimaryImageUrl, x.Product.Status, x.Product.FandomId)
        )).ToList();
    }

    public async Task<(bool Success, string? Error, AuctionDetailItem? Data)> GetDetailAsync(long auctionId, CancellationToken ct)
    {
        var item = await _auctions.GetByIdWithRelationsAsync(auctionId, ct);
        if (item is null) return (false, "Auction not found.", null);

        return (true, null, new AuctionDetailItem(
            item.Id,
            item.ProductId,
            item.SellerId,
            item.FloorPrice,
            item.ReservePrice,
            item.CurrentBid,
            item.Status,
            item.EndsAt,
            item.ExtensionSeconds,
            item.TriggerBeforeEnd,
            new AuctionSellerSummary(item.Seller.Id, item.Seller.Username, item.Seller.FullName, item.Seller.AvatarUrl, item.Seller.AvgSellerRating, item.Seller.TotalRatings),
            new AuctionProductSummary(item.Product.Id, item.Product.Name, item.Product.Artist, item.Product.Type, item.Product.Price, item.Product.Stock, item.Product.PrimaryImageUrl, item.Product.Status, item.Product.FandomId)
        ));
    }

    public async Task<IReadOnlyList<BidHistoryItem>> GetBidHistoryAsync(long auctionId, CancellationToken ct)
    {
        var bids = await _bids.GetByAuctionIdAsync(auctionId, ct);
        return bids.Select(x => new BidHistoryItem(
            x.Id,
            x.AuctionId,
            x.Amount,
            x.IsWinning,
            x.PlacedAt,
            new BidderSnapshot(x.UserId, x.User.Username, x.User.AvatarUrl, x.User.AvatarBg)
        )).ToList();
    }

    public async Task<(bool Success, string? Error, PlaceBidResponse? Data)> PlaceBidAsync(long bidderId, long auctionId, PlaceBidRequest request, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdWithProductAsync(auctionId, ct);
        if (auction is null) return (false, "Auction not found.", null);
        if (auction.Status is not (AuctionStatus.Live or AuctionStatus.Extended)) return (false, "Auction is not live.", null);
        if (auction.EndsAt <= DateTimeOffset.UtcNow) return (false, "Auction has ended.", null);
        if (auction.SellerId == bidderId) return (false, "Seller cannot bid own auction.", null);

        var highest = await _bids.GetHighestBidAmountAsync(auctionId, ct);

        decimal minRequired;
        if (highest is null)
        {
            // No bids yet — first bid must be >= floor price
            minRequired = auction.FloorPrice;
        }
        else
        {
            // Subsequent bids must exceed current highest by at least 1,000 VND
            minRequired = highest.Value + 1_000m;
        }

        if (request.Amount < minRequired)
            return (false, $"Bid must be >= {minRequired:N0} VND.", null);

        var bid = new Bid
        {
            AuctionId = auction.Id,
            UserId = bidderId,
            Amount = request.Amount,
            IsWinning = true,
            PlacedAt = DateTimeOffset.UtcNow
        };

        // Notify the previous winner that they've been outbid
        var previousTopBid = await _bids.GetTopBidAsync(auctionId, ct);
        if (previousTopBid is not null && previousTopBid.UserId != bidderId)
        {
            _notifications.Add(_notificationFactory.Outbid(
                previousTopBid.UserId,
                auctionId,
                auction.Product.Name,
                request.Amount));
        }

        await _bids.ClearWinningFlagsAsync(auction.Id, ct);
        _bids.Add(bid);

        auction.CurrentBid = request.Amount;

        var remaining = auction.EndsAt - DateTimeOffset.UtcNow;
        if (remaining.TotalSeconds <= auction.TriggerBeforeEnd)
        {
            auction.EndsAt = auction.EndsAt.AddSeconds(auction.ExtensionSeconds);
            auction.ExtensionCount += 1;
            auction.Status = AuctionStatus.Extended;
            // Reset reminder flag so the 5-min reminder fires again after extension
            auction.ReminderSent5Min = false;
        }
        else
        {
            auction.Status = AuctionStatus.Live;
        }

        auction.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        return (true, null, new PlaceBidResponse(auction.Id, bid.Id, bid.Amount, auction.CurrentBid, auction.Status));
    }
}
