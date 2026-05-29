using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Hubs;

/// <summary>
/// Authenticated hub — each user joins their personal group "chat-user-{userId}".
/// Server pushes "ReceiveMessage" and "ConversationRead" events to these groups.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        await base.OnDisconnectedAsync(exception);
    }

    public static string UserGroup(long userId) => $"chat-user-{userId}";

    private long? GetUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub");
        return long.TryParse(sub, out var id) ? id : null;
    }
}
