using System.Security.Claims;
using EzBias.Application.Features.Ratings;
using EzBias.Application.Features.Ratings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api")]
public class RatingsController : ControllerBase
{
    private readonly IRatingApplicationService _ratings;

    public RatingsController(IRatingApplicationService ratings)
    {
        _ratings = ratings;
    }

    [Authorize]
    [HttpPost("ratings")]
    public async Task<IActionResult> Create([FromBody] CreateRatingRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _ratings.CreateAsync(userId, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Order not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("users/{id:long}/ratings")]
    public async Task<IActionResult> GetBySeller([FromRoute] long id, CancellationToken ct)
        => Ok(await _ratings.GetBySellerAsync(id, ct));

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
