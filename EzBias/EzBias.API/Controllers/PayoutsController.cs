using EzBias.Application.Features.Payouts;
using EzBias.Application.Features.Payouts.Dtos;
using EzBias.API.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/admin/payouts")]
[Authorize(Roles = "Admin")]
public class PayoutsController : ControllerBase
{
    private readonly IPayoutApplicationService _service;

    public PayoutsController(IPayoutApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] EzBias.Domain.Enums.PayoutStatus? status, CancellationToken ct)
    {
        var data = await _service.GetAdminPayoutsAsync(status, ct);
        return Ok(data);
    }

    [HttpPut("{payoutId:long}/approve")]
    public async Task<IActionResult> Approve([FromRoute] long payoutId, [FromBody] MarkPayoutPaidRequest request, CancellationToken ct)
    {
        var result = await _service.MarkPaidAsync(payoutId, request, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPut("{payoutId:long}/reject")]
    public async Task<IActionResult> Reject([FromRoute] long payoutId, [FromBody] RejectPayoutRequest request, CancellationToken ct)
    {
        var result = await _service.RejectAsync(payoutId, request, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }
}
