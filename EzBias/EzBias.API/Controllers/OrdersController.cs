using System.Security.Claims;
using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orders;
    private readonly IEscrowRepository _escrows;
    private readonly IPayoutRepository _payouts;
    private readonly IUnitOfWork _uow;

    public OrdersController(IOrderRepository orders, IEscrowRepository escrows, IPayoutRepository payouts, IUnitOfWork uow)
    {
        _orders = orders;
        _escrows = escrows;
        _payouts = payouts;
        _uow = uow;
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
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound("Order not found.");
        if (order.UserId != userId) return Forbid();
        if (order.Status != OrderStatus.Shipped && order.Status != OrderStatus.Delivered)
            return BadRequest("Order cannot be confirmed in current status.");

        order.DeliveredAt = DateTimeOffset.UtcNow;
        order.CompletedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _escrows.AddRange(new[]
        {
            new EscrowTransaction
            {
                OrderId = order.Id,
                SellerId = order.SellerId,
                Type = EscrowType.OUT,
                Amount = order.Total,
                CreatedAt = DateTimeOffset.UtcNow
            }
        });

        var payout = await _payouts.GetByOrderIdAsync(order.Id, ct);
        if (payout is null)
        {
            _payouts.Add(new Payout
            {
                OrderId = order.Id,
                SellerId = order.SellerId,
                Amount = order.Total,
                Status = PayoutStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _uow.SaveChangesAsync(ct);
        return Ok(new FulfillmentActionResponse(order.Id, order.Status.ToString()));
    }

    private static object Map(EzBias.Domain.Entities.Order o) => new
    {
        o.Id, o.UserId, o.SellerId, o.Source, o.AuctionId, o.Total, o.Status, o.AddressSnap, o.Carrier, o.TrackingNumber, o.CreatedAt,
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
