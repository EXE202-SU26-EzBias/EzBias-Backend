using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

public class AuctionHub : Hub
{
    public async Task JoinAuction(long auctionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));

    public async Task LeaveAuction(long auctionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));

    public static string AuctionGroup(long auctionId) => $"auction-{auctionId}";
}
