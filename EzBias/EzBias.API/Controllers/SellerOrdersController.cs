using System.Security.Claims;
using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Enums;
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
    private readonly IUnitOfWork _uow;

    public SellerOrdersController(IOrderRepository orders, IUnitOfWork uow)
    {
        _orders = orders;
        _uow = uow;
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
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound("Order not found.");
        if (order.SellerId != userId) return Forbid();
        if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Processing)
            return BadRequest("Order cannot be marked shipped in current status.");

        order.Carrier = request.Carrier?.Trim();
        order.TrackingNumber = $"TRK-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{order.Id}";
        order.ShippedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Shipped;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return Ok(new FulfillmentActionResponse(order.Id, order.Status.ToString()));
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
