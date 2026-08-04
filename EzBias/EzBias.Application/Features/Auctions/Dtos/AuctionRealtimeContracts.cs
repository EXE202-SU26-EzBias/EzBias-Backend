namespace EzBias.Application.Features.Auctions.Dtos;

public record BidPlacedEvent(
    long AuctionId,
    long BidId,
    decimal Amount,
    decimal CurrentBid,
    string Status,
    DateTimeOffset PlacedAt);
