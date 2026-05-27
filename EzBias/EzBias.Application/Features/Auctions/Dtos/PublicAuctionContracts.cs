using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions.Dtos;

public record AuctionSellerSummary(long Id, string Username, string FullName, string AvatarUrl, decimal AvgSellerRating, int TotalRatings);
public record AuctionProductSummary(long Id, string Name, string Artist, string Type, decimal Price, int Stock, string PrimaryImageUrl, ProductStatus Status, string FandomId);

public record AuctionListItem(
    long AuctionId,
    long ProductId,
    long SellerId,
    decimal FloorPrice,
    decimal CurrentBid,
    AuctionStatus Status,
    DateTimeOffset EndsAt,
    AuctionSellerSummary Seller,
    AuctionProductSummary Product);

public record AuctionDetailItem(
    long AuctionId,
    long ProductId,
    long SellerId,
    decimal FloorPrice,
    decimal? ReservePrice,
    decimal CurrentBid,
    AuctionStatus Status,
    DateTimeOffset EndsAt,
    int ExtensionSeconds,
    int TriggerBeforeEnd,
    AuctionSellerSummary Seller,
    AuctionProductSummary Product,
    long? WinnerId = null);

public record PlaceBidRequest(decimal Amount);
public record PlaceBidResponse(long AuctionId, long BidId, decimal Amount, decimal CurrentBid, AuctionStatus Status);
