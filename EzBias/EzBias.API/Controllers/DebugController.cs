using EzBias.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/debug")]
[Authorize]
public class DebugController : ControllerBase
{
    private readonly EzBiasDbContext _db;
    private readonly IWebHostEnvironment _env;

    public DebugController(EzBiasDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("seed-products")]
    public async Task<IActionResult> GetSeedProducts(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var data = await _db.Products
            .Where(x => x.Name.StartsWith("[SEED]"))
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Price,
                x.Stock,
                x.SellerId,
                x.Status,
                x.IsAuction,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(data);
    }
}
