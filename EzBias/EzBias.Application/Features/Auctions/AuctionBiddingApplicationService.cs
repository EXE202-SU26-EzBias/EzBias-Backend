using EzBias.Application.Common.Results;
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
    private readonly IAuctionDepositRepository _deposits;
    private readonly IUnitOfWork _uow;
    private readonly IAuctionRealtime _auctionRealtime;

    public AuctionBiddingApplicationService(
        IAuctionRepository auctions,
        IBidRepository bids,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IAuctionDepositRepository deposits,
        IUnitOfWork uow,
        IAuctionRealtime auctionRealtime)
    {
        _auctions = auctions;
        _bids = bids;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _deposits = deposits;
        _uow = uow;
        _auctionRealtime = auctionRealtime;
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

    public async Task<Result<AuctionDetailItem>> GetDetailAsync(long auctionId, CancellationToken ct)
    {
        var item = await _auctions.GetByIdWithRelationsAsync(auctionId, ct);
        if (item is null) return Result<AuctionDetailItem>.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);

        return Result<AuctionDetailItem>.Ok(new AuctionDetailItem(
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
            new AuctionProductSummary(item.Product.Id, item.Product.Name, item.Product.Artist, item.Product.Type, item.Product.Price, item.Product.Stock, item.Product.PrimaryImageUrl, item.Product.Status, item.Product.FandomId),
            item.WinnerId
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

    public async Task<Result<PlaceBidResponse>> PlaceBidAsync(long bidderId, long auctionId, PlaceBidRequest request, CancellationToken ct)
    {
        PlaceBidResponse response;
        BidPlacedEvent realtimeEvent;

        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            var now = DateTimeOffset.UtcNow;

            var auction = await _auctions.GetByIdWithProductForUpdateAsync(auctionId, ct);
            if (auction is null) return Result<PlaceBidResponse>.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);
            if (auction.Status is not (AuctionStatus.Live or AuctionStatus.Extended)) return Result<PlaceBidResponse>.Fail("Auction is not live.", ApplicationErrorCode.Validation);
            if (auction.EndsAt <= DateTimeOffset.UtcNow) return Result<PlaceBidResponse>.Fail("Auction has ended.", ApplicationErrorCode.Validation);
            if (auction.SellerId == bidderId) return Result<PlaceBidResponse>.Fail("Seller cannot bid own auction.", ApplicationErrorCode.Validation);

            if (auction.RequiredDepositAmount > 0m)
            {
                var hasHeld = await _deposits.HasHeldDepositAsync(bidderId, auctionId, ct);
                if (!hasHeld)
                    return Result<PlaceBidResponse>.Fail("A held deposit is required to bid on this auction.", ApplicationErrorCode.Validation);
            }

            var highest = await _bids.GetHighestBidAmountAsync(auctionId, ct);

            decimal minRequired;
            if (highest is null)
            {
                minRequired = auction.FloorPrice;
            }
            else
            {
                minRequired = highest.Value + 1_000m;
            }

            if (request.Amount < minRequired)
                return Result<PlaceBidResponse>.Fail($"Bid must be >= {minRequired:N0} VND.", ApplicationErrorCode.Validation);

            var bid = new Bid
            {
                AuctionId = auction.Id,
                UserId = bidderId,
                Amount = request.Amount,
                IsWinning = true,
                PlacedAt = DateTimeOffset.UtcNow
            };

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

            var transition = auction.RecordBid(request.Amount, now);
            if (transition == TransitionOutcome.Invalid)
                return Result<PlaceBidResponse>.Fail("Auction is not live.", ApplicationErrorCode.Validation);

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            response = new PlaceBidResponse(auction.Id, bid.Id, bid.Amount, auction.CurrentBid, auction.Status);
            realtimeEvent = new BidPlacedEvent(
                auction.Id,
                bid.Id,
                bid.Amount,
                auction.CurrentBid,
                auction.Status.ToString(),
                bid.PlacedAt);
        }

        await _auctionRealtime.PushBidPlacedAsync(realtimeEvent, ct);
        return Result<PlaceBidResponse>.Ok(response);
    }
}
