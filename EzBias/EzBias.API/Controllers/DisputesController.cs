using System.Security.Claims;
using EzBias.API.Mappings;
using EzBias.Application.Common.Results;
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPut("{id:long}/refund-payment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteRefundPayment([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        var result = await _service.CompleteRefundPaymentAsync(adminId, id, new CompleteRefundPaymentRequest(), ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
