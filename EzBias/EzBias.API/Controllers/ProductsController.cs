using System.Security.Claims;
using EzBias.API.Mappings;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Media;
using EzBias.Application.Features.Products;
using EzBias.Application.Features.Products.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductManagementApplicationService _service;
    private readonly IImageUploader _imageUploader;

    public ProductsController(IProductManagementApplicationService service, IImageUploader imageUploader)
    {
        _service = service;
        _imageUploader = imageUploader;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _service.GetMineAsync(userId, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.GetMineByIdAsync(userId, id, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateProductFormRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.FandomName))
        {
            if (!FandomNameNormalizer.TryNormalize(request.FandomName, out _, out _, out var fandomError))
                return BadRequest(new { message = fandomError });
        }
        else if (string.IsNullOrWhiteSpace(request.FandomId))
        {
            return BadRequest(new { message = "Fandom is required." });
        }

        if (request.Images.Count == 0)
            return BadRequest(new { message = "At least one product image is required." });
        if (request.Images.Count > 8)
            return BadRequest(new { message = "A maximum of 8 images are allowed." });

        List<string> uploadedUrls;
        try
        {
            var uploads = request.Images.Select(img => UploadImageAsync(img, ct));
            uploadedUrls = [.. await Task.WhenAll(uploads)];
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var createRequest = new CreateProductRequest(
            request.FandomName,
            request.FandomId,
            request.Artist,
            request.Name,
            request.Type,
            request.Condition,
            request.Price,
            request.Stock,
            request.Description,
            uploadedUrls[0],
            uploadedUrls);

        var result = await _service.CreateAsync(userId, createRequest, ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result, notFoundAsBadRequest: true);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update([FromRoute] long id, [FromForm] UpdateProductFormRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request.Images.Count > 8)
            return BadRequest(new { message = "A maximum of 8 images are allowed." });

        List<string> newImageUrls = [];
        if (request.Images.Count > 0)
        {
            try
            {
                var uploads = request.Images.Select(img => UploadImageAsync(img, ct));
                newImageUrls = [.. await Task.WhenAll(uploads)];
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        var updateRequest = new UpdateProductRequest(
            request.Price,
            request.Stock,
            request.Description,
            request.Status,
            newImageUrls.Count > 0 ? newImageUrls : null,
            request.ReplaceImages ? (request.KeepImageUrls ?? []) : null);

        var result = await _service.UpdateAsync(userId, id, updateRequest, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.DeleteAsync(userId, id, ct);
        if (!result.IsSuccess)
            return result.Failure?.Kind == ErrorKind.Forbidden
                ? this.ToErrorActionResult(result)
                : this.ToErrorActionResult(result, forceKind: ErrorKind.NotFound);
        return NoContent();
    }

    private async Task<string> UploadImageAsync(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return await _imageUploader.UploadProductImageAsync(
            new UploadFile(
                stream,
                file.FileName,
                file.ContentType,
                file.Length),
            ct);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}

public sealed class CreateProductFormRequest
{
    public string? FandomName { get; set; }
    public string? FandomId { get; set; }
    public string Artist { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ProductCondition Condition { get; set; } = ProductCondition.Good;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<IFormFile> Images { get; set; } = [];
}

public sealed class UpdateProductFormRequest
{
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public List<IFormFile> Images { get; set; } = [];
    /// <summary>
    /// When true, existing images are replaced by KeepImageUrls + new uploads.
    /// When false/absent, all existing images are kept and new uploads are appended.
    /// </summary>
    public bool ReplaceImages { get; set; }
    /// <summary>
    /// URLs of existing images to keep. Only used when ReplaceImages = true.
    /// </summary>
    public List<string>? KeepImageUrls { get; set; }
}
