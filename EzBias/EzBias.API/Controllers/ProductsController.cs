using System.Security.Claims;
using EzBias.Application.Features.Products;
using EzBias.Application.Features.Products.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductManagementApplicationService _service;

    public ProductsController(IProductManagementApplicationService service)
    {
        _service = service;
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
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CreateAsync(userId, request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result.Error);
        return Ok(result.Data);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.UpdateAsync(userId, id, request, ct);
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
