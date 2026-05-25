using EzBias.Infrastructure.Persistence;
using EzBias.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly EzBiasDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DebugController(EzBiasDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
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

    /// <summary>
    /// Truncates all data (except EF migration history) and re-seeds.
    /// Requires header: X-Debug-Secret matching Debug:ResetSecret in config.
    /// </summary>
    [HttpPost("reset-and-reseed")]
    public async Task<IActionResult> ResetAndReseed(CancellationToken ct)
    {
        var secret = _config["Debug:ResetSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return NotFound();

        if (!Request.Headers.TryGetValue("X-Debug-Secret", out var provided) ||
            provided.ToString() != secret)
            return Unauthorized(new { message = "Invalid secret." });

        // Truncate all tables in dependency order, keep __EFMigrationsHistory
        await _db.Database.ExecuteSqlRawAsync(@"
            SET session_replication_role = 'replica';
            TRUNCATE TABLE
                otp_verifications, refresh_tokens, notifications, ratings,
                dispute_items, disputes, refunds, commission_transactions,
                escrow_transactions, payouts, payment_orders, payments,
                order_items, orders, bids, auctions, cart_items, wishlists,
                seller_follows, product_images, product_boosts, products,
                contact_messages, fandoms, users
            RESTART IDENTITY CASCADE;
            SET session_replication_role = 'DEFAULT';
        ", ct);

        // Re-seed
        await ProductSeedData.SeedAsync(_db, ct);
        var sellers = ProductSeedData.GetSeedSellers(_db);
        await AuctionSeedData.SeedAsync(_db, sellers, ct);

        return Ok(new { message = "Database reset and re-seeded successfully." });
    }
}
