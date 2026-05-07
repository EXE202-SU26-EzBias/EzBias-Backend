using EzBias.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IProductRepository _products;
    private readonly IFandomRepository _fandoms;

    public CatalogController(IProductRepository products, IFandomRepository fandoms)
    {
        _products = products;
        _fandoms = fandoms;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? fandomId, CancellationToken ct)
    {
        var items = await _products.GetActiveAsync(fandomId, ct);
        var data = items.Select(x => new
        {
            x.Id,
            x.SellerId,
            x.FandomId,
            x.Artist,
            x.Name,
            x.Type,
            x.Price,
            x.Stock,
            x.PrimaryImageUrl,
            x.IsAuction,
            x.Status,
            x.CreatedAt
        });
        return Ok(data);
    }

    [HttpGet("fandoms")]
    public async Task<IActionResult> GetFandoms(CancellationToken ct)
    {
        var items = await _fandoms.GetActiveAsync(ct);
        var data = items.Select(x => new { x.Id, x.Name, x.IsActive });
        return Ok(data);
    }
}
