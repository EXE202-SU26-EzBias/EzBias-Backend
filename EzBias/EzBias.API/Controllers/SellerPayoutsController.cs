using System.Security.Claims;
using EzBias.Application.Features.Payouts;
using EzBias.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/seller/payouts")]
[Authorize]
public class SellerPayoutsController : ControllerBase
{
    private readonly IPayoutApplicationService _service;

    public SellerPayoutsController(IPayoutApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PayoutStatus? status, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _service.GetSellerPayoutsAsync(userId, status, ct);
        return Ok(data);
    }

    [HttpPost("request")]
    public async Task<IActionResult> Request([FromQuery] long orderId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.RequestAsync(userId, orderId, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Order not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
