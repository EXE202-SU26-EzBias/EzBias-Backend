using System.Security.Claims;
using EzBias.Application.Features.Notifications;
using EzBias.Application.Features.Orders;
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
    private readonly IOrderApplicationService _orderService;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;

    public SellerOrdersController(
        IOrderRepository orders,
        IOrderApplicationService orderService,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow)
    {
        _orders = orders;
        _orderService = orderService;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = await _orderService.GetBySellerAsync(userId, ct);
        return Ok(items);
    }

    [HttpPut("{id:long}/ship")]
    public async Task<IActionResult> Ship([FromRoute] long id, [FromBody] MarkShippedRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound(new { message = "Order not found." });
        if (order.SellerId != userId) return Forbid();
        if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Processing)
            return BadRequest(new { message = "Order cannot be marked shipped in current status." });

        order.Carrier = request.Carrier?.Trim();
        order.TrackingNumber = $"TRK-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{order.Id}";
        order.ShippedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Shipped;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _notifications.Add(_notificationFactory.OrderShipped(order.UserId, order.Id, order.TrackingNumber));

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
