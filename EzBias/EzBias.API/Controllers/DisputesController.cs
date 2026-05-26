using System.Security.Claims;
using EzBias.Application.Features.Disputes;
using EzBias.Application.Features.Disputes.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
public class DisputesController : ControllerBase
{
    private readonly IDisputeApplicationService _service;

    public DisputesController(IDisputeApplicationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDisputeRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CreateAsync(userId, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Order not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetList(CancellationToken ct)
    {
        var items = await _service.GetListAsync(ct);
        return Ok(items);
    }

    [HttpPut("{id:long}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve([FromRoute] long id, [FromBody] ResolveDisputeRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        var result = await _service.ApproveAsync(adminId, id, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Dispute not found." || result.Error == "Order not found." || result.Error == "Payment not found for order.") return NotFound(new { message = result.Error });
            if (result.Error == "Payout already paid. Manual recovery required.") return Conflict(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:long}/refund-payment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteRefundPayment([FromRoute] long id, [FromBody] CompleteRefundPaymentRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        var result = await _service.CompleteRefundPaymentAsync(adminId, id, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Dispute not found." || result.Error == "Refund not found for dispute." || result.Error == "Order not found." || result.Error == "Payment not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id:long}/items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetItems([FromRoute] long id, CancellationToken ct)
    {
        var disputes = await _service.GetListAsync(ct);
        var items = disputes.FirstOrDefault(x => x.Id == id);
        if (items is null) return NotFound(new { message = "Dispute not found." });
        return Ok(items.Items);
    }

    [HttpPut("{id:long}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject([FromRoute] long id, [FromBody] RejectDisputeRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        var result = await _service.RejectAsync(adminId, id, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Dispute not found." || result.Error == "Order not found.") return NotFound(new { message = result.Error });
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
