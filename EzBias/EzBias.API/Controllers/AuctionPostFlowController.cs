using System.Security.Claims;
using EzBias.Application.Features.Auctions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/auction-post")]
[Authorize]
public class AuctionPostFlowController : ControllerBase
{
    private readonly IAuctionPostFlowQueryService _service;

    public AuctionPostFlowController(IAuctionPostFlowQueryService service)
    {
        _service = service;
    }

    [HttpGet("buyer/won")]
    public async Task<IActionResult> BuyerWon([FromQuery] bool onlyPendingPayment = false, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _service.GetBuyerWonAsync(userId, onlyPendingPayment, ct);
        return Ok(data);
    }

    [HttpGet("seller/ended")]
    public async Task<IActionResult> SellerEnded(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _service.GetSellerEndedAsync(userId, ct);
        return Ok(data);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
