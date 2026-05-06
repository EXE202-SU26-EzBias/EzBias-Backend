using EzBias.Application.Features.Payouts;
using EzBias.Application.Features.Payouts.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/payouts")]
[Authorize(Roles = "Admin")]
public class PayoutsController : ControllerBase
{
    private readonly IPayoutApplicationService _service;

    public PayoutsController(IPayoutApplicationService service)
    {
        _service = service;
    }

    [HttpPost("{payoutId:long}/mark-paid")]
    public async Task<IActionResult> MarkPaid([FromRoute] long payoutId, [FromBody] MarkPayoutPaidRequest request, CancellationToken ct)
    {
        var result = await _service.MarkPaidAsync(payoutId, request, ct);
        if (!result.Success || result.Data is null)
            return NotFound(result.Error);

        return Ok(result.Data);
    }
}
