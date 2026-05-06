using System.Security.Claims;
using EzBias.Application.Features.Checkout;
using EzBias.Application.Features.Checkout.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/checkout")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutApplicationService _checkoutService;

    public CheckoutController(ICheckoutApplicationService checkoutService)
    {
        _checkoutService = checkoutService;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] CheckoutPreviewRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _checkoutService.PreviewAsync(userId, request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] CheckoutSubmitRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _checkoutService.SubmitAsync(userId, request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result.Error);

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
