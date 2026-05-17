using System.Security.Claims;
using EzBias.Application.Features.Orders;
using EzBias.Application.Features.Orders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderApplicationService _orderService;

    public OrdersController(IOrderApplicationService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _orderService.CreateAsync(userId, request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result.Error);
        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = await _orderService.GetByBuyerAsync(userId, ct);
        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _orderService.GetDetailAsync(userId, id, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Order not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:long}/confirm")]
    public async Task<IActionResult> Confirm([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _orderService.ConfirmReceivedAsync(userId, id, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Order not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _orderService.DeleteAsync(userId, id, ct);
        if (!result.Success)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Order not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
