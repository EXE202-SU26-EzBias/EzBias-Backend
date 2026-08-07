using System.Security.Claims;
using EzBias.API.Mappings;
using EzBias.Application.Features.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationApplicationService _notifications;

    public NotificationsController(INotificationApplicationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _notifications.GetMyAsync(userId, ct));
    }

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _notifications.MarkReadAsync(userId, id, ct);
        if (!result.IsSuccess)
            return this.ToErrorActionResult(result);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkReadAll(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _notifications.MarkReadAllAsync(userId, ct);
        if (!result.IsSuccess)
            return this.ToErrorActionResult(result);

        return Ok(new { updated = result.Value });
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
