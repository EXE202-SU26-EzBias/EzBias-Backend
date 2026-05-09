using EzBias.Application.Features.Products;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogQueryService _catalog;

    public CatalogController(ICatalogQueryService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? fandomId, CancellationToken ct)
    {
        var data = await _catalog.GetProductsAsync(fandomId, ct);
        return Ok(data);
    }

    [HttpGet("fandoms")]
    public async Task<IActionResult> GetFandoms(CancellationToken ct)
    {
        var data = await _catalog.GetFandomsAsync(ct);
        return Ok(data);
    }
}
