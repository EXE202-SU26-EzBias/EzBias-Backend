using System.Security.Claims;
using EzBias.Application.Features.Orders;
using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/seller/orders")]
[Authorize]
public class SellerOrdersController : ControllerBase
{
    private readonly IOrderRepository _orders;
    private readonly IOrderFulfillmentApplicationService _fulfillment;

    public SellerOrdersController(IOrderRepository orders, IOrderFulfillmentApplicationService fulfillment)
    {
        _orders = orders;
        _fulfillment = fulfillment;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = await _orders.GetBySellerAsync(userId, ct);
        return Ok(items.Select(x => new { x.Id, x.UserId, x.Total, x.Status, x.Carrier, x.TrackingNumber, x.CreatedAt }));
    }

    [HttpPut("{id:long}/ship")]
    public async Task<IActionResult> Ship([FromRoute] long id, [FromBody] MarkShippedRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _fulfillment.MarkShippedAsync(userId, id, request, ct);
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
