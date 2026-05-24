using System.Security.Claims;
using EzBias.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/seller/dashboard")]
[Authorize]
public class SellerDashboardController : ControllerBase
{
    private readonly IUserProfileApplicationService _service;

    public SellerDashboardController(IUserProfileApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _service.GetSellerDashboardAsync(userId, ct);
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
