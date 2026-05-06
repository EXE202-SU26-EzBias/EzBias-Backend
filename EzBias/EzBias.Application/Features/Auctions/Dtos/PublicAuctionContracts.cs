using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions.Dtos;

public record AuctionListItem(long AuctionId, long ProductId, long SellerId, decimal FloorPrice, decimal CurrentBid, AuctionStatus Status, DateTimeOffset EndsAt);
public record AuctionDetailItem(long AuctionId, long ProductId, long SellerId, decimal FloorPrice, decimal? ReservePrice, decimal CurrentBid, AuctionStatus Status, DateTimeOffset EndsAt, int ExtensionSeconds, int TriggerBeforeEnd);
public record PlaceBidRequest(decimal Amount);
public record PlaceBidResponse(long AuctionId, long BidId, decimal Amount, decimal CurrentBid, AuctionStatus Status);
