using System.Security.Claims;
using EzBias.API.Mappings;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Media;
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
    [RequestSizeLimit(80 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 80 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromRoute] long productId, [FromForm] ProductReviewFormRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var (media, streams) = OpenMediaStreams(request.Media);
        try
        {
            var result = await _reviews.CreateAsync(
                userId,
                productId,
                new CreateProductReviewRequest(request.Stars, request.Comment, media),
                ct);
            if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
            return Ok(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            await DisposeStreamsAsync(streams);
        }
    }

    [Authorize]
    [HttpPut("reviews/{id:long}")]
    [RequestSizeLimit(80 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 80 * 1024 * 1024)]
    public async Task<IActionResult> Update([FromRoute] long id, [FromForm] ProductReviewFormRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var (media, streams) = OpenMediaStreams(request.Media);
        try
        {
            var result = await _reviews.UpdateAsync(
                userId,
                id,
                new UpdateProductReviewRequest(request.Stars, request.Comment, request.KeepMediaIds, media),
                ct);
            if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
            return Ok(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            await DisposeStreamsAsync(streams);
        }
    }

    [Authorize]
    [HttpDelete("reviews/{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _reviews.DeleteAsync(userId, id, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result);
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
        if (!result.IsSuccess) return this.ToErrorActionResult(result);
        return NoContent();
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }

    private static (List<UploadFile> Media, List<Stream> Streams) OpenMediaStreams(IReadOnlyList<IFormFile> files)
    {
        var streams = files.Select(file => file.OpenReadStream()).ToList();
        var media = files.Select((file, index) => new UploadFile(
            streams[index],
            file.FileName,
            file.ContentType,
            file.Length)).ToList();
        return (media, streams);
    }

    private static async Task DisposeStreamsAsync(IEnumerable<Stream> streams)
    {
        foreach (var stream in streams)
            await stream.DisposeAsync();
    }
}

public sealed class ProductReviewFormRequest
{
    public short Stars { get; set; }
    public string? Comment { get; set; }
    public List<IFormFile> Media { get; set; } = [];
    public List<long> KeepMediaIds { get; set; } = [];
}
