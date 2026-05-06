using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Infrastructure.Persistence.SeedData;

public static class ProductSeedData
{
    public static async Task SeedAsync(EzBiasDbContext db, CancellationToken ct = default)
    {
        var fandomId = "bts";

        if (!db.Fandoms.Any(x => x.Id == fandomId))
        {
            db.Fandoms.Add(new Fandom
            {
                Id = fandomId,
                Name = "BTS",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        var seller = db.Users.FirstOrDefault(x => x.Email == "seller.demo@ezbias.local");
        if (seller is null)
        {
            seller = new User
            {
                FullName = "Demo Seller",
                Username = "demo_seller",
                Email = "seller.demo@ezbias.local",
                PasswordHash = "SEED_ONLY_NOT_FOR_LOGIN",
                Role = UserRole.User,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(seller);
            await db.SaveChangesAsync(ct);
        }

        if (db.Products.Any(x => x.SellerId == seller.Id && x.Name.StartsWith("[SEED]")))
            return;

        var now = DateTimeOffset.UtcNow;
        db.Products.AddRange(
            new Product
            {
                SellerId = seller.Id,
                FandomId = fandomId,
                Artist = "Jungkook",
                Name = "[SEED] Golden Photocard A",
                Type = "Photocard",
                Condition = ProductCondition.New,
                Price = 180000,
                Stock = 10,
                Description = "Seed data product for cart/checkout testing.",
                PrimaryImageUrl = "https://picsum.photos/seed/ezbias1/600/600",
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new Product
            {
                SellerId = seller.Id,
                FandomId = fandomId,
                Artist = "V",
                Name = "[SEED] Layover Album Ver.2",
                Type = "Album",
                Condition = ProductCondition.LikeNew,
                Price = 320000,
                Stock = 5,
                Description = "Seed data product for API testing.",
                PrimaryImageUrl = "https://picsum.photos/seed/ezbias2/600/600",
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new Product
            {
                SellerId = seller.Id,
                FandomId = fandomId,
                Artist = "Jimin",
                Name = "[SEED] FACE Poster",
                Type = "Merch",
                Condition = ProductCondition.Good,
                Price = 90000,
                Stock = 20,
                Description = "Seed data merch for cart flow.",
                PrimaryImageUrl = "https://picsum.photos/seed/ezbias3/600/600",
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            }
        );

        await db.SaveChangesAsync(ct);
    }
}
