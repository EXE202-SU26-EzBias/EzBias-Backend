using System.Security.Claims;
using EzBias.Application.Features.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentApplicationService _paymentService;

    public PaymentsController(IPaymentApplicationService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("{paymentId:long}/confirm")]
    public async Task<IActionResult> Confirm([FromRoute] long paymentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _paymentService.ConfirmAsync(userId, paymentId, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            return NotFound(result.Error);
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
