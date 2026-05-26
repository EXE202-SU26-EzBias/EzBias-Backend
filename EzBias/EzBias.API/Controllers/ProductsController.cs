using System.Security.Claims;
using EzBias.API.Integrations;
using EzBias.Application.Features.Products;
using EzBias.Application.Features.Products.Dtos;
using EzBias.Domain.Enums;
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
        if (!result.Success || result.Data is null)
            return result.Error == "Forbidden." ? Forbid() : NotFound(result.Error);
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateProductFormRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (Request.Form.Files.Count > 1)
            return BadRequest(new { message = "Only one product image is allowed." });
        if (request.Image is null)
            return BadRequest(new { message = "Product image is required." });

        string uploadedUrl;
        try
        {
            uploadedUrl = await _imageUploader.UploadProductImageAsync(request.Image, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var createRequest = new CreateProductRequest(
            request.FandomId,
            request.Artist,
            request.Name,
            request.Type,
            request.Condition,
            request.Price,
            request.Stock,
            request.Description,
            uploadedUrl,
            [uploadedUrl]);

        var result = await _service.CreateAsync(userId, createRequest, ct);
        if (!result.Success || result.Data is null) return BadRequest(new { message = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update([FromRoute] long id, [FromForm] UpdateProductFormRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        string? newImageUrl = null;
        if (request.Image is not null)
        {
            try
            {
                newImageUrl = await _imageUploader.UploadProductImageAsync(request.Image, ct);
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
            newImageUrl);

        var result = await _service.UpdateAsync(userId, id, updateRequest, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Product not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.DeleteAsync(userId, id, ct);
        if (!result.Success)
            return result.Error == "Forbidden." ? Forbid() : NotFound(result.Error);
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

public sealed class CreateProductFormRequest
{
    public string FandomId { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ProductCondition Condition { get; set; } = ProductCondition.Good;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}

public sealed class UpdateProductFormRequest
{
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public IFormFile? Image { get; set; }
}
