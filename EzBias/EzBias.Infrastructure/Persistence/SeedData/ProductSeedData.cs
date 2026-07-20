using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Infrastructure.Persistence.SeedData;

public static class ProductSeedData
{
    private const string SeedPrefix = "demo_seller_";

    private static readonly IReadOnlyDictionary<string, string[]> SeedProductImages = new Dictionary<string, string[]>
    {
        ["BTS V Lotte Layover Photo Card"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526490/Card_%E1%BA%A3nh_Lotte_Layover_V_BTS_i0ivpu.jpg"
        ],
        ["BLACKPINK Polaroid Set"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526487/Polaroid_BlackpinK_fkrgrv.jpg"
        ],
        ["BTS Merch Box 3"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526486/WTS_BTS_MERCH_BOX_3_x6dfbn.jpg"
        ],
        ["BTS 220pcs Lomo Card Pack"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526486/220_Pcs_BTS_Album_Photocards_BTS_Lomo_Cards_Kpop_Merchandise_oxavqe.jpg"
        ],
        ["Jennie Rounded Corner Card Set"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526486/10_card_gi%E1%BA%A5y_bo_g%C3%B3c_Jennie_ug9zax.jpg"
        ],
        ["CORTIS Color Outside The Lines EP"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526486/Pre-order_CORTIS_The_1st_EP_COLOR_OUTSIDE_THE_LINES_u2o7pj.jpg"
        ],
        ["SNSD Mini Light"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526485/Mini_Light_SNSD_Girls_Generation_zdrhtp.jpg"
        ],
        ["CORTIS Color Outside The Lines Photo Album"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526485/B%E1%BB%99_%E1%BA%A3nh_album_CORTIS_-_COLOR_OUTSIDE_THE_LINES_c%C3%B3_s%E1%BA%B5n_aksqxm.jpg"
        ],
        ["BLACKPINK How You Like That T-Shirt"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526485/%C3%81O_Blackpink_How_You_Like_That_b6slx2.jpg"
        ],
        ["CORTIS Rounded Lomo Card Set"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526485/LOMO_CARD_BO_G%C3%93C_CORTIS_lpgvse.jpg"
        ],
        ["BLACKPINK Deadline Silver Version"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526484/BLACKPINK_3rd_Mini_Album_Deadline_Silver_Version_gdbb5k.jpg"
        ],
        ["BLACKPINK Photo Album"] =
        [
            "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526484/Album_%E1%BA%A3nh_blackpink_t4d0cb.jpg"
        ]
    };

    // Maps old [SEED] names and other legacy names to current names for migration
    private static readonly IReadOnlyDictionary<string, string> LegacySeedProductNames = new Dictionary<string, string>
    {
        ["[SEED] BTS V Lotte Layover Photo Card"]           = "BTS V Lotte Layover Photo Card",
        ["[SEED] BLACKPINK Polaroid Set"]                   = "BLACKPINK Polaroid Set",
        ["[SEED] BTS Merch Box 3"]                          = "BTS Merch Box 3",
        ["[SEED] BTS 220pcs Lomo Card Pack"]                = "BTS 220pcs Lomo Card Pack",
        ["[SEED] Jennie Rounded Corner Card Set"]           = "Jennie Rounded Corner Card Set",
        ["[SEED] CORTIS Color Outside The Lines EP"]        = "CORTIS Color Outside The Lines EP",
        ["[SEED] SNSD Mini Light"]                          = "SNSD Mini Light",
        ["[SEED] CORTIS Color Outside The Lines Photo Album"] = "CORTIS Color Outside The Lines Photo Album",
        ["[SEED] BLACKPINK How You Like That T-Shirt"]      = "BLACKPINK How You Like That T-Shirt",
        ["[SEED] CORTIS Rounded Lomo Card Set"]             = "CORTIS Rounded Lomo Card Set",
        ["[SEED] BLACKPINK Deadline Silver Version"]        = "BLACKPINK Deadline Silver Version",
        ["[SEED] BLACKPINK Photo Album"]                    = "BLACKPINK Photo Album",
        ["[SEED] Golden Photocard A"]                       = "BTS V Lotte Layover Photo Card",
        ["[SEED] Layover Album Ver.2"]                      = "BLACKPINK Polaroid Set",
        ["[SEED] FACE Poster"]                              = "BTS Merch Box 3",
        ["[SEED] Indigo Album Blue Ver."]                   = "BTS 220pcs Lomo Card Pack",
        ["[SEED] D-DAY Photobook"]                          = "Jennie Rounded Corner Card Set",
        ["[SEED] Jack In The Box Card Set"]                 = "CORTIS Color Outside The Lines EP",
        ["[SEED] ME Photocard Set"]                         = "SNSD Mini Light",
        ["[SEED] Get Up Album Bunny Beach Bag Ver."]        = "CORTIS Color Outside The Lines Photo Album",
        ["[SEED] NA Album Digipack"]                        = "BLACKPINK How You Like That T-Shirt"
    };

    public static async Task SeedAsync(EzBiasDbContext db, CancellationToken ct = default)
    {
        var fandomSeeds = new List<Fandom>
        {
            new() { Id = "bts",       Name = "BTS",               IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = "blackpink", Name = "BLACKPINK",          IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = "newjeans",  Name = "NewJeans",           IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = "twice",     Name = "TWICE",              IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = "cortis",    Name = "CORTIS",             IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = "snsd",      Name = "Girls' Generation",  IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
        };

        var existingFandomIds = db.Fandoms.Select(x => x.Id).ToHashSet();
        var fandomsToInsert = fandomSeeds.Where(x => !existingFandomIds.Contains(x.Id)).ToList();
        if (fandomsToInsert.Count > 0)
        {
            db.Fandoms.AddRange(fandomsToInsert);
            await db.SaveChangesAsync(ct);
        }

        var sellers = await EnsureSeedSellersAsync(db, ct);

        var now = DateTimeOffset.UtcNow;

        var candidates = new List<Product>
        {
            new()
            {
                SellerId = sellers[0].Id,
                FandomId = "bts",
                Artist = "V",
                Name = "BTS V Lotte Layover Photo Card",
                Type = "Photocard",
                Condition = ProductCondition.New,
                Price = 180_000,
                Stock = 10,
                Description = "Photocard chính hãng của V (BTS) từ sự kiện Lotte x Layover. Card còn mới, chưa qua sử dụng, kèm sleeve bảo vệ.",
                PrimaryImageUrl = GetPrimarySeedImage("BTS V Lotte Layover Photo Card"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[0].Id,
                FandomId = "blackpink",
                Artist = "BLACKPINK",
                Name = "BLACKPINK Polaroid Set",
                Type = "Photocard",
                Condition = ProductCondition.LikeNew,
                Price = 125_000,
                Stock = 12,
                Description = "Bộ ảnh polaroid BLACKPINK gồm 4 thành viên, in màu sắc nét, kích thước chuẩn polaroid. Tình trạng như mới, không trầy xước.",
                PrimaryImageUrl = GetPrimarySeedImage("BLACKPINK Polaroid Set"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[1].Id,
                FandomId = "bts",
                Artist = "BTS",
                Name = "BTS Merch Box 3",
                Type = "Merch",
                Condition = ProductCondition.Good,
                Price = 450_000,
                Stock = 4,
                Description = "Hộp merch BTS số 3 bao gồm nhiều phụ kiện chính hãng: keychain, sticker, postcard và mini poster. Đã qua sử dụng nhẹ, còn đầy đủ phụ kiện.",
                PrimaryImageUrl = GetPrimarySeedImage("BTS Merch Box 3"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[1].Id,
                FandomId = "bts",
                Artist = "BTS",
                Name = "BTS 220pcs Lomo Card Pack",
                Type = "Photocard",
                Condition = ProductCondition.New,
                Price = 220_000,
                Stock = 7,
                Description = "Bộ 220 lomo card BTS tổng hợp từ nhiều album, in chất lượng cao. Đóng gói cẩn thận, còn nguyên seal.",
                PrimaryImageUrl = GetPrimarySeedImage("BTS 220pcs Lomo Card Pack"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[2].Id,
                FandomId = "blackpink",
                Artist = "Jennie",
                Name = "Jennie Rounded Corner Card Set",
                Type = "Photocard",
                Condition = ProductCondition.LikeNew,
                Price = 115_000,
                Stock = 15,
                Description = "Bộ 10 card giấy bo góc hình Jennie (BLACKPINK), in ảnh đẹp, màu sắc tươi sáng. Tình trạng như mới.",
                PrimaryImageUrl = GetPrimarySeedImage("Jennie Rounded Corner Card Set"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[2].Id,
                FandomId = "cortis",
                Artist = "CORTIS",
                Name = "CORTIS Color Outside The Lines EP",
                Type = "Album",
                Condition = ProductCondition.Good,
                Price = 360_000,
                Stock = 6,
                Description = "EP đầu tay của CORTIS — Color Outside The Lines. Bao gồm CD, photobook và photocard ngẫu nhiên. Đã bóc seal, tình trạng tốt.",
                PrimaryImageUrl = GetPrimarySeedImage("CORTIS Color Outside The Lines EP"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[0].Id,
                FandomId = "snsd",
                Artist = "Girls' Generation",
                Name = "SNSD Mini Light",
                Type = "Merch",
                Condition = ProductCondition.New,
                Price = 250_000,
                Stock = 8,
                Description = "Đèn mini light chính hãng của Girls' Generation (SNSD), phát sáng nhiều màu. Còn nguyên hộp, pin mới, chưa qua sử dụng.",
                PrimaryImageUrl = GetPrimarySeedImage("SNSD Mini Light"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[1].Id,
                FandomId = "cortis",
                Artist = "CORTIS",
                Name = "CORTIS Color Outside The Lines Photo Album",
                Type = "Album",
                Condition = ProductCondition.New,
                Price = 320_000,
                Stock = 6,
                Description = "Bộ ảnh album CORTIS — Color Outside The Lines còn sẵn hàng. Ảnh in chất lượng cao, bìa cứng, còn nguyên seal.",
                PrimaryImageUrl = GetPrimarySeedImage("CORTIS Color Outside The Lines Photo Album"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[2].Id,
                FandomId = "blackpink",
                Artist = "BLACKPINK",
                Name = "BLACKPINK How You Like That T-Shirt",
                Type = "Apparel",
                Condition = ProductCondition.LikeNew,
                Price = 190_000,
                Stock = 11,
                Description = "Áo thun BLACKPINK How You Like That, chất liệu cotton thoáng mát, in logo và hình ảnh MV. Đã giặt 1 lần, không phai màu, còn rất đẹp.",
                PrimaryImageUrl = GetPrimarySeedImage("BLACKPINK How You Like That T-Shirt"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[0].Id,
                FandomId = "cortis",
                Artist = "CORTIS",
                Name = "CORTIS Rounded Lomo Card Set",
                Type = "Photocard",
                Condition = ProductCondition.New,
                Price = 135_000,
                Stock = 18,
                Description = "Bộ lomo card bo góc của CORTIS, in ảnh các thành viên chất lượng cao. Còn nguyên seal, chưa bóc.",
                PrimaryImageUrl = GetPrimarySeedImage("CORTIS Rounded Lomo Card Set"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[1].Id,
                FandomId = "blackpink",
                Artist = "BLACKPINK",
                Name = "BLACKPINK Deadline Silver Version",
                Type = "Album",
                Condition = ProductCondition.New,
                Price = 420_000,
                Stock = 5,
                Description = "Mini album thứ 3 của BLACKPINK — Deadline phiên bản Silver. Bao gồm CD, photobook, photocard ngẫu nhiên và poster. Còn nguyên seal chưa bóc.",
                PrimaryImageUrl = GetPrimarySeedImage("BLACKPINK Deadline Silver Version"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            },
            new()
            {
                SellerId = sellers[2].Id,
                FandomId = "blackpink",
                Artist = "BLACKPINK",
                Name = "BLACKPINK Photo Album",
                Type = "Album",
                Condition = ProductCondition.Good,
                Price = 300_000,
                Stock = 9,
                Description = "Album ảnh BLACKPINK tổng hợp, bìa cứng, in ảnh 4 thành viên chất lượng cao. Tình trạng tốt, không rách hay ố vàng.",
                PrimaryImageUrl = GetPrimarySeedImage("BLACKPINK Photo Album"),
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now
            }
        };

        await UpgradeLegacySeedProductsAsync(db, candidates, ct);

        var existingSeedProductNames = db.Products
            .Where(x => SeedProductImages.Keys.Contains(x.Name))
            .Select(x => x.Name)
            .ToHashSet();

        var toInsert = candidates
            .Where(x => !existingSeedProductNames.Contains(x.Name))
            .ToList();

        if (toInsert.Count > 0)
        {
            db.Products.AddRange(toInsert);
            await db.SaveChangesAsync(ct);
        }

        await EnsureSeedProductImagesAsync(db, candidates, ct);
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
                    EmailVerifiedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Users.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                var changed = false;
                var needsReset = false;
                try { needsReset = !BCrypt.Net.BCrypt.Verify("Seller@123", existing.PasswordHash); }
                catch (BCrypt.Net.SaltParseException) { needsReset = true; }

                if (needsReset) { existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Seller@123"); changed = true; }
                if (existing.EmailVerifiedAt is null) { existing.EmailVerifiedAt = DateTimeOffset.UtcNow; changed = true; }
                if (changed) { existing.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); }
            }

            users.Add(existing);
        }

        return users;
    }

    public static List<User> GetSeedSellers(EzBiasDbContext db)
        => db.Users
            .Where(x => x.Email.StartsWith("seller") && x.Email.EndsWith("@ezbias.local"))
            .OrderBy(x => x.Email)
            .ToList();

    private static string GetPrimarySeedImage(string productName)
        => SeedProductImages.TryGetValue(productName, out var urls) && urls.Length > 0
            ? urls[0]
            : string.Empty;

    private static async Task EnsureSeedProductImagesAsync(EzBiasDbContext db, IEnumerable<Product> seedProducts, CancellationToken ct)
    {
        var seedProductNames = seedProducts.Select(x => x.Name).ToHashSet();
        var products = db.Products.Where(x => seedProductNames.Contains(x.Name)).ToList();
        if (products.Count == 0) return;

        var changed = false;
        var productIds = products.Select(x => x.Id).ToList();
        var existingImages = db.ProductImages.Where(x => productIds.Contains(x.ProductId)).ToList();

        foreach (var product in products)
        {
            if (!SeedProductImages.TryGetValue(product.Name, out var urls) || urls.Length == 0)
                continue;

            if (product.PrimaryImageUrl != urls[0])
            {
                product.PrimaryImageUrl = urls[0];
                product.UpdatedAt = DateTimeOffset.UtcNow;
                changed = true;
            }

            for (var index = 0; index < urls.Length; index++)
            {
                var sortOrder = (short)(index + 1);
                var url = urls[index];
                var imageExists = existingImages.Any(x =>
                    x.ProductId == product.Id && (x.Url == url || x.SortOrder == sortOrder));
                if (imageExists) continue;

                db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Url = url,
                    SortOrder = sortOrder,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                changed = true;
            }
        }

        if (changed) await db.SaveChangesAsync(ct);
    }

    private static async Task UpgradeLegacySeedProductsAsync(EzBiasDbContext db, IReadOnlyList<Product> currentSeeds, CancellationToken ct)
    {
        var changed = false;

        foreach (var (legacyName, currentName) in LegacySeedProductNames)
        {
            var existing = db.Products.FirstOrDefault(x => x.Name == legacyName);
            var current = currentSeeds.FirstOrDefault(x => x.Name == currentName);

            if (existing is null || current is null || db.Products.Any(x => x.Name == currentName))
                continue;

            existing.SellerId = current.SellerId;
            existing.FandomId = current.FandomId;
            existing.Artist = current.Artist;
            existing.Name = current.Name;
            existing.Type = current.Type;
            existing.Condition = current.Condition;
            existing.Price = current.Price;
            existing.Stock = Math.Max(existing.Stock, current.Stock);
            existing.Description = current.Description;
            existing.PrimaryImageUrl = current.PrimaryImageUrl;
            existing.IsAuction = current.IsAuction;
            existing.Status = current.Status;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            changed = true;
        }

        if (changed) await db.SaveChangesAsync(ct);
    }
}
