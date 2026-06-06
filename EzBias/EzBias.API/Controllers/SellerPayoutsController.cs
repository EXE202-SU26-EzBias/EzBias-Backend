using System.Security.Claims;
using EzBias.Application.Features.Payouts;
using EzBias.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/seller/payouts")]
[Authorize]
public class SellerPayoutsController : ControllerBase
{
    private readonly IPayoutApplicationService _service;

    public SellerPayoutsController(IPayoutApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PayoutStatus? status, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _service.GetSellerPayoutsAsync(userId, status, ct);
        return Ok(data);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
