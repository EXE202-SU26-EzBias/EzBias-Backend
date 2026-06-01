using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions.Dtos;

public record CreateAuctionRequest(long ProductId, decimal FloorPrice, decimal? ReservePrice, DateTimeOffset EndsAt, bool IsUrgent = false, bool HasProofImage = false, int ExtensionSeconds = 300, int TriggerBeforeEnd = 60);
public record RelistAuctionRequest(decimal FloorPrice, decimal? ReservePrice, DateTimeOffset EndsAt, bool IsUrgent = false, bool HasProofImage = false, int ExtensionSeconds = 300, int TriggerBeforeEnd = 60);
public record AuctionActionResponse(long AuctionId, string Status);
public record SellerAuctionItem(
    long AuctionId,
    long ProductId,
    decimal FloorPrice,
    decimal CurrentBid,
    AuctionStatus Status,
    DateTimeOffset EndsAt,
    DateTimeOffset CreatedAt,
    AuctionProductSummary Product,
    long? RelistedToAuctionId);
