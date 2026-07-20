using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Persistence.SeedData;

public static class SeedDataRunner
{
    public static async Task<SeedDataResult> RunAsync(
        EzBiasDbContext db,
        SeedDataOptions options,
        bool applyMigrations = true,
        CancellationToken ct = default)
    {
        options.Validate();

        if (applyMigrations)
            await db.Database.MigrateAsync(ct);

        await AdminSeedData.SeedAsync(db, options.Admin, ct);

        if (!options.Enabled)
            return new SeedDataResult(false, "AdminOnly");

        await ProductSeedData.SeedAsync(db, ct);

        var sellers = ProductSeedData.GetSeedSellers(db);
        await AuctionSeedData.SeedAsync(db, sellers, ct);
        await SalesSeedData.SeedAsync(db, ct);
        await ProductReviewSeedData.SeedAsync(db, ct);
        await TransactionSeedData.SeedAsync(db, ct);

        return new SeedDataResult(true, "AdminAndDemoData");
    }
}

public sealed record SeedDataResult(bool DemoSeedEnabled, string Mode);
