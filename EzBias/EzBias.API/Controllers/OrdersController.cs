using System.Security.Claims;
using EzBias.Application.Features.Checkout;
using EzBias.Application.Features.Checkout.Dtos;
using EzBias.Application.Features.Orders;
using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ICheckoutApplicationService _checkout;
    private readonly IOrderRepository _orders;
    private readonly IOrderFulfillmentApplicationService _fulfillment;

    public OrdersController(ICheckoutApplicationService checkout, IOrderRepository orders, IOrderFulfillmentApplicationService fulfillment)
    {
        _checkout = checkout;
        _orders = orders;
        _fulfillment = fulfillment;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CheckoutSubmitRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _checkout.SubmitAsync(userId, request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result.Error);
        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = await _orders.GetByBuyerAsync(userId, ct);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var order = await _orders.GetByIdWithItemsAsync(id, ct);
        if (order is null) return NotFound("Order not found.");
        if (order.UserId != userId && order.SellerId != userId) return Forbid();
        return Ok(Map(order));
    }

    [HttpPut("{id:long}/confirm")]
    public async Task<IActionResult> Confirm([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _fulfillment.ConfirmReceivedAsync(userId, id, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    private static object Map(EzBias.Domain.Entities.Order o) => new
    {
        o.Id, o.UserId, o.SellerId, o.Source, o.AuctionId, o.Total, o.ShippingFee, o.Status, o.AddressSnap, o.Carrier, o.TrackingNumber, o.CreatedAt,
        Items = o.Items.Select(i => new { i.Id, i.ProductId, i.ProductName, i.ProductImage, i.Quantity, i.UnitPrice, i.Subtotal })
    };

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
