using EzBias.Application.Features.Auctions;
using EzBias.Application.Features.Auctions.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

public sealed class SignalRAuctionRealtime : IAuctionRealtime
{
    private readonly IHubContext<AuctionHub> _hub;
    private readonly ILogger<SignalRAuctionRealtime> _logger;

    public SignalRAuctionRealtime(
        IHubContext<AuctionHub> hub,
        ILogger<SignalRAuctionRealtime> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PushBidPlacedAsync(
        BidPlacedEvent eventData,
        CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients
                .Group(AuctionHub.AuctionGroup(eventData.AuctionId))
                .SendAsync("BidPlaced", new
                {
                    auctionId = eventData.AuctionId,
                    bidId = eventData.BidId,
                    amount = eventData.Amount,
                    currentBid = eventData.CurrentBid,
                    status = eventData.Status,
                    placedAt = eventData.PlacedAt
                }, ct);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                ex,
                "BidPlaced broadcast canceled for auction {AuctionId}, bid {BidId}.",
                eventData.AuctionId,
                eventData.BidId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "BidPlaced broadcast failed for auction {AuctionId}, bid {BidId}.",
                eventData.AuctionId,
                eventData.BidId);
        }
    }
}
