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

        await EnsureAdminUserAsync(db, ct);
        var sellers = await EnsureSeedSellersAsync(db, ct);

        var existingSeedProductNames = db.Products
            .Where(x => x.Name.StartsWith("[SEED]"))
            .Select(x => x.Name)
            .ToHashSet();

        var now = DateTimeOffset.UtcNow;

        var candidates = new List<Product>
        {
            new()
            {
                SellerId = sellers[0].Id,
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
            new()
            {
                SellerId = sellers[0].Id,
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
            new()
            {
                SellerId = sellers[1].Id,
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
            },
            new()
            {
                SellerId = sellers[1].Id,
                FandomId = fandomId,
                Artist = "RM",
                Name = "[SEED] Indigo Album Blue Ver.",
                Type = "Album",
                Condition = ProductCondition.New,
                Price = 280000,
                Stock = 7,
                Description = "Second seller sample album.",
                PrimaryImageUrl = "https://picsum.photos/seed/ezbias4/600/600",
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[2].Id,
                FandomId = fandomId,
                Artist = "SUGA",
                Name = "[SEED] D-DAY Photobook",
                Type = "Merch",
                Condition = ProductCondition.LikeNew,
                Price = 210000,
                Stock = 9,
                Description = "Third seller sample merch.",
                PrimaryImageUrl = "https://picsum.photos/seed/ezbias5/600/600",
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[2].Id,
                FandomId = fandomId,
                Artist = "J-Hope",
                Name = "[SEED] Jack In The Box Card Set",
                Type = "Photocard",
                Condition = ProductCondition.Good,
                Price = 130000,
                Stock = 15,
                Description = "Third seller sample card set.",
                PrimaryImageUrl = "https://picsum.photos/seed/ezbias6/600/600",
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            }
        };

        var toInsert = candidates
            .Where(x => !existingSeedProductNames.Contains(x.Name))
            .ToList();

        if (toInsert.Count == 0)
            return;

        db.Products.AddRange(toInsert);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureAdminUserAsync(EzBiasDbContext db, CancellationToken ct)
    {
        const string adminEmail = "admin.demo@ezbias.local";
        const string adminPassword = "Admin@123";

        var admin = db.Users.FirstOrDefault(x => x.Email == adminEmail);
        if (admin is null)
        {
            admin = new User
            {
                FullName = "Demo Admin",
                Username = "demo_admin",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = UserRole.Admin,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
            return;
        }

        var changed = false;
        if (admin.Role != UserRole.Admin)
        {
            admin.Role = UserRole.Admin;
            changed = true;
        }

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(adminPassword, admin.PasswordHash))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                changed = true;
            }
        }
        catch (BCrypt.Net.SaltParseException)
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            changed = true;
        }

        if (changed)
        {
            admin.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task<List<User>> EnsureSeedSellersAsync(EzBiasDbContext db, CancellationToken ct)
    {
        var seeds = new List<(string FullName, string Username, string Email)>
        {
            ("Demo Seller 1", "demo_seller_1", "seller1.demo@ezbias.local"),
            ("Demo Seller 2", "demo_seller_2", "seller2.demo@ezbias.local"),
            ("Demo Seller 3", "demo_seller_3", "seller3.demo@ezbias.local")
        };

        var users = new List<User>();

        foreach (var seed in seeds)
        {
            var existing = db.Users.FirstOrDefault(x => x.Email == seed.Email);
            if (existing is null)
            {
                existing = new User
                {
                    FullName = seed.FullName,
                    Username = seed.Username,
                    Email = seed.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Seller@123"),
                    Role = UserRole.User,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Users.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                var needsReset = false;
                try
                {
                    needsReset = !BCrypt.Net.BCrypt.Verify("Seller@123", existing.PasswordHash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Legacy/non-bcrypt seed hash (e.g. "SEED_ONLY_NOT_FOR_LOGIN")
                    needsReset = true;
                }

                if (needsReset)
                {
                    existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Seller@123");
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
            }

            users.Add(existing);
        }

        return users;
    }
}
