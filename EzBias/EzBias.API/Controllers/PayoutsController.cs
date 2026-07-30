using EzBias.Application.Features.Payouts;
using EzBias.Application.Features.Payouts.Dtos;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
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
        var typed = result.ToResult();
        if (!typed.IsSuccess || typed.Value is null) return this.ToErrorActionResult(typed, notFoundAsBadRequest: true);
        return Ok(typed.Value);
    }

    [HttpPut("{payoutId:long}/reject")]
    public async Task<IActionResult> Reject([FromRoute] long payoutId, [FromBody] RejectPayoutRequest request, CancellationToken ct)
    {
        var result = await _service.RejectAsync(payoutId, request, ct);
        var typed = result.ToResult();
        if (!typed.IsSuccess || typed.Value is null) return this.ToErrorActionResult(typed, notFoundAsBadRequest: true);
        return Ok(typed.Value);
    }
}
