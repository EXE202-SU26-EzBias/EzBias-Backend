using EzBias.Application.Features.Auctions.Dtos;

namespace EzBias.Application.Features.Auctions;

public interface IAuctionRealtime
{
    Task PushBidPlacedAsync(
        BidPlacedEvent eventData,
        CancellationToken ct = default);
}
