namespace EzBias.Application.Features.Auctions.Dtos;

public record BidderSnapshot(long Id, string Username, string AvatarUrl, string AvatarBg);
public record BidHistoryItem(long BidId, long AuctionId, decimal Amount, bool IsWinning, DateTimeOffset PlacedAt, BidderSnapshot Bidder);
