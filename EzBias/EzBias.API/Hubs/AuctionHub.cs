using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

/// <summary>
/// Public hub — no [Authorize] required so anonymous viewers can join.
/// Clients call JoinAuction / LeaveAuction to subscribe to a specific auction room.
/// Server pushes "BidPlaced" events to the room when a new bid is accepted.
/// </summary>
public class AuctionHub : Hub
{
    public async Task JoinAuction(long auctionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));

    public async Task LeaveAuction(long auctionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));

    public static string AuctionGroup(long auctionId) => $"auction-{auctionId}";
}
