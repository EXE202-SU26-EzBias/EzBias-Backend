using EzBias.Infrastructure.Persistence;
using EzBias.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly EzBiasDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly SeedDataOptions _seedOptions;

    public DebugController(
        EzBiasDbContext db,
        IWebHostEnvironment env,
        IConfiguration config,
        IOptions<SeedDataOptions> seedOptions)
    {
        _db = db;
        _env = env;
        _config = config;
        _seedOptions = seedOptions.Value;
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

    [HttpGet("check-config")]
    public IActionResult CheckConfig()
    {
        var secret = _config["Debug:ResetSecret"];
        return Ok(new
        {
            hasSecret = !string.IsNullOrWhiteSpace(secret),
            secretLength = secret?.Length ?? 0,
            secretPreview = string.IsNullOrWhiteSpace(secret) ? "(empty)" : secret[..Math.Min(3, secret.Length)] + "***"
        });
    }

    /// <summary>
    /// Lightweight health endpoint for operational probes.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow,
            uptime = Environment.TickCount64 / 1000.0 // seconds since app start
        });
    }

    /// <summary>
    /// Truncates all data (except EF migration history) and re-seeds.
    /// Requires header: X-Debug-Secret matching Debug:ResetSecret in config.
    /// </summary>
    [HttpPost("reset-and-reseed")]
    public async Task<IActionResult> ResetAndReseed(
        [FromHeader(Name = "X-Debug-Secret")] string? secret,
        CancellationToken ct)
    {
        var configSecret = _config["Debug:ResetSecret"];
        if (string.IsNullOrWhiteSpace(configSecret))
            return NotFound();

        if (string.IsNullOrWhiteSpace(secret) || secret != configSecret)
            return Unauthorized(new { message = "Invalid secret." });

        try
        {
            _seedOptions.Validate();
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            // Truncate all tables using CASCADE (handles FK automatically)
            await _db.Database.ExecuteSqlRawAsync(@"
                TRUNCATE TABLE
                    otp_verifications, refresh_tokens, notifications,
                    dispute_items, disputes, refunds, commission_transactions,
                    escrow_transactions, payouts, payment_orders, payments,
                    order_items, orders, bids, auction_deposits, auctions, cart_items, wishlists,
                    seller_follows, product_images, product_boosts, product_reviews, products,
                    contact_messages, fandoms, users
                RESTART IDENTITY CASCADE;
            ", ct);

            var seedResult = await SeedDataRunner.RunAsync(
                _db,
                _seedOptions,
                applyMigrations: false,
                ct: ct);
            await transaction.CommitAsync(ct);

            return Ok(new
            {
                message = seedResult.DemoSeedEnabled
                    ? "Database reset and demo data seeded successfully."
                    : "Database reset and Admin account seeded successfully.",
                demoSeedEnabled = seedResult.DemoSeedEnabled,
                seedMode = seedResult.Mode
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
        }
    }
}
