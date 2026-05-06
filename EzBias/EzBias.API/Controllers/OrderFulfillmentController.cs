using System.Security.Claims;
using EzBias.Application.Features.Orders;
using EzBias.Application.Features.Orders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderFulfillmentController : ControllerBase
{
    private readonly IOrderFulfillmentApplicationService _service;

    public OrderFulfillmentController(IOrderFulfillmentApplicationService service)
    {
        _service = service;
    }

    [HttpPost("{orderId:long}/mark-shipped")]
    public async Task<IActionResult> MarkShipped([FromRoute] long orderId, [FromBody] MarkShippedRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _service.MarkShippedAsync(userId, orderId, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("{orderId:long}/confirm-received")]
    public async Task<IActionResult> ConfirmReceived([FromRoute] long orderId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _service.ConfirmReceivedAsync(userId, orderId, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            return BadRequest(result.Error);
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
