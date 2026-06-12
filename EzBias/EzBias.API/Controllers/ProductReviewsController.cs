using System.Security.Claims;
using EzBias.Application.Features.Reviews;
using EzBias.Application.Features.Reviews.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api")]
public class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewApplicationService _reviews;

    public ProductReviewsController(IProductReviewApplicationService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet("products/{productId:long}/reviews")]
    public async Task<IActionResult> GetByProduct([FromRoute] long productId, CancellationToken ct)
        => Ok(await _reviews.GetSummaryAsync(productId, ct));

    [Authorize]
    [HttpGet("products/{productId:long}/reviews/eligibility")]
    public async Task<IActionResult> GetEligibility([FromRoute] long productId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _reviews.GetEligibilityAsync(userId, productId, ct));
    }

    [Authorize]
    [HttpPost("products/{productId:long}/reviews")]
    public async Task<IActionResult> Create([FromRoute] long productId, [FromBody] CreateProductReviewRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _reviews.CreateAsync(userId, productId, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Product not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [Authorize]
    [HttpPut("reviews/{id:long}")]
    public async Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateProductReviewRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _reviews.UpdateAsync(userId, id, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Review not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [Authorize]
    [HttpDelete("reviews/{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _reviews.DeleteAsync(userId, id, ct);
        if (!result.Success)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Review not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/reviews")]
    public async Task<IActionResult> AdminGetAll(CancellationToken ct)
    {
        var items = await _reviews.GetAllForAdminAsync(ct);
        return Ok(items);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/reviews/{id:long}")]
    public async Task<IActionResult> AdminDelete([FromRoute] long id, CancellationToken ct)
    {
        var result = await _reviews.AdminDeleteAsync(id, ct);
        if (!result.Success)
        {
            if (result.Error == "Review not found.") return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
