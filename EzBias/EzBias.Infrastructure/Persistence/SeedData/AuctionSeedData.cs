using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Services;

namespace EzBias.Infrastructure.Persistence.SeedData;

public static class AuctionSeedData
{
    private static readonly IReadOnlyList<(string FullName, string Username, string Email)> BidderSeeds = new List<(string, string, string)>
    {
        ("Minh Khoa", "minhkhoa_fan",   "bidder1.demo@ezbias.local"),
        ("Thu Hà",   "thuha_kpop",      "bidder2.demo@ezbias.local"),
        ("Quang Huy", "quanghuy_bias",  "bidder3.demo@ezbias.local"),
        ("Lan Anh",  "lananh_collect",  "bidder4.demo@ezbias.local"),
        ("Tuấn Kiệt", "tuankiet_merch", "bidder5.demo@ezbias.local")
    };

    private static readonly IReadOnlyList<AuctionSeedEntry> Entries = new List<AuctionSeedEntry>
    {
        new(
            SellerIndex: 0,
            FandomId: "twice",
            Artist: "TWICE",
            ProductName: "TWICE Ready To Be Tour T-Shirt",
            Type: "Apparel",
            Condition: ProductCondition.LikeNew,
            Description: "Áo phông chính hãng từ tour diễn Ready To Be của TWICE. Chất liệu cotton cao cấp, in logo tour và hình ảnh các thành viên. Đã qua sử dụng 1 lần, còn rất mới, không phai màu.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636016/TWICE_Ready_To_Be_Tour_T-Shirt_r7ub1d.jpg",
            FloorPrice: 350_000m,
            ReservePrice: 500_000m,
            DurationHours: 24,
            BidHistory: new[]
            {
                (BidderIndex: 0, Amount: 350_000m, MinutesAgo: 300),
                (BidderIndex: 2, Amount: 380_000m, MinutesAgo: 240),
                (BidderIndex: 0, Amount: 420_000m, MinutesAgo: 180),
                (BidderIndex: 3, Amount: 460_000m, MinutesAgo: 120),
                (BidderIndex: 2, Amount: 490_000m, MinutesAgo: 75),
                (BidderIndex: 0, Amount: 520_000m, MinutesAgo: 40),
                (BidderIndex: 3, Amount: 550_000m, MinutesAgo: 10)
            }
        ),
        new(
            SellerIndex: 1,
            FandomId: "newjeans",
            Artist: "NewJeans",
            ProductName: "NewJeans OMG Photocard Set",
            Type: "Photocard",
            Condition: ProductCondition.New,
            Description: "Bộ photocard đầy đủ 5 thành viên từ album OMG của NewJeans. Tất cả card còn nguyên seal, chưa bóc. Bao gồm Minji, Hanni, Danielle, Haerin và Hyein.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636016/NewJeans_OMG_Photocard_Set_rhacx9.jpg",
            FloorPrice: 200_000m,
            ReservePrice: 350_000m,
            DurationHours: 48,
            BidHistory: new[]
            {
                (BidderIndex: 1, Amount: 200_000m, MinutesAgo: 200),
                (BidderIndex: 4, Amount: 230_000m, MinutesAgo: 150),
                (BidderIndex: 1, Amount: 260_000m, MinutesAgo: 100),
                (BidderIndex: 3, Amount: 290_000m, MinutesAgo: 50),
                (BidderIndex: 4, Amount: 320_000m, MinutesAgo: 15)
            }
        ),
        new(
            SellerIndex: 2,
            FandomId: "blackpink",
            Artist: "BLACKPINK",
            ProductName: "BLACKPINK Lightstick Ver.2",
            Type: "Merch",
            Condition: ProductCondition.Good,
            Description: "Lightstick chính hãng BLACKPINK phiên bản 2. Đã sử dụng tại concert, còn hoạt động tốt, đèn sáng đều, pin mới thay. Kèm hộp đựng gốc và dây đeo.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636015/BLACKPINK_Lightstick_Ver.2_doxxrs.jpg",
            FloorPrice: 450_000m,
            ReservePrice: 700_000m,
            DurationHours: 36,
            BidHistory: new[]
            {
                (BidderIndex: 2, Amount: 450_000m, MinutesAgo: 480),
                (BidderIndex: 0, Amount: 490_000m, MinutesAgo: 400),
                (BidderIndex: 3, Amount: 540_000m, MinutesAgo: 320),
                (BidderIndex: 2, Amount: 590_000m, MinutesAgo: 240),
                (BidderIndex: 1, Amount: 630_000m, MinutesAgo: 160),
                (BidderIndex: 0, Amount: 660_000m, MinutesAgo: 90),
                (BidderIndex: 3, Amount: 700_000m, MinutesAgo: 40),
                (BidderIndex: 1, Amount: 730_000m, MinutesAgo: 8)
            }
        ),
        new(
            SellerIndex: 0,
            FandomId: "straykids",
            Artist: "Stray Kids",
            ProductName: "Stray Kids SKZOO Plush",
            Type: "Merch",
            Condition: ProductCondition.New,
            Description: "Thú bông SKZOO chính hãng của Stray Kids, phiên bản giới hạn. Còn nguyên tag, chưa qua sử dụng. Kích thước 25cm, chất liệu bông mềm mại. Phù hợp trưng bày hoặc làm quà tặng.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636015/Stray_Kids_SKZOO_Plush_vfbmjv.jpg",
            FloorPrice: 280_000m,
            ReservePrice: 450_000m,
            DurationHours: 72,
            BidHistory: new[]
            {
                (BidderIndex: 4, Amount: 280_000m, MinutesAgo: 180),
                (BidderIndex: 2, Amount: 310_000m, MinutesAgo: 90),
                (BidderIndex: 4, Amount: 340_000m, MinutesAgo: 30)
            }
        ),
        new(
            SellerIndex: 1,
            FandomId: "bts",
            Artist: "BTS",
            ProductName: "BTS Butter Album Peaches Version",
            Type: "Album",
            Condition: ProductCondition.New,
            Description: "Album Butter phiên bản Peaches của BTS, còn nguyên seal chưa bóc. Bao gồm CD, photobook 80 trang, photocard ngẫu nhiên, mini poster và sticker sheet. Hàng chính hãng HYBE.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636015/BTS_Butter_Album_Peaches_Version_kqm2w6.jpg",
            FloorPrice: 300_000m,
            ReservePrice: 500_000m,
            DurationHours: 48,
            BidHistory: new[]
            {
                (BidderIndex: 4, Amount: 300_000m, MinutesAgo: 360),
                (BidderIndex: 1, Amount: 340_000m, MinutesAgo: 280),
                (BidderIndex: 4, Amount: 380_000m, MinutesAgo: 200),
                (BidderIndex: 0, Amount: 420_000m, MinutesAgo: 120),
                (BidderIndex: 1, Amount: 460_000m, MinutesAgo: 60),
                (BidderIndex: 4, Amount: 500_000m, MinutesAgo: 20)
            }
        ),
        new(
            SellerIndex: 2,
            FandomId: "blackpink",
            Artist: "BLACKPINK",
            ProductName: "BLACKPINK Deadline Mood Light Ver.",
            Type: "Merch",
            Condition: ProductCondition.New,
            Description: "Đèn mood light phiên bản giới hạn từ album Deadline của BLACKPINK. Thiết kế hình đồng hồ cát pha lê phát sáng màu hồng tím đặc trưng. Còn nguyên hộp, chưa qua sử dụng. Hàng chính hãng YG Entertainment.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636964/BLACKPINK_-_DEADLINE_LIMITED_MOOD_LIGHT_VER._fjrf3a.jpg",
            FloorPrice: 800_000m,
            ReservePrice: 1_200_000m,
            DurationHours: 48,
            BidHistory: new[]
            {
                (BidderIndex: 0, Amount:   800_000m, MinutesAgo: 600),
                (BidderIndex: 3, Amount:   880_000m, MinutesAgo: 500),
                (BidderIndex: 0, Amount:   960_000m, MinutesAgo: 420),
                (BidderIndex: 2, Amount: 1_040_000m, MinutesAgo: 340),
                (BidderIndex: 3, Amount: 1_080_000m, MinutesAgo: 260),
                (BidderIndex: 1, Amount: 1_120_000m, MinutesAgo: 180),
                (BidderIndex: 0, Amount: 1_160_000m, MinutesAgo: 100),
                (BidderIndex: 2, Amount: 1_200_000m, MinutesAgo: 40),
                (BidderIndex: 3, Amount: 1_250_000m, MinutesAgo: 5)
            }
        ),
        new(
            SellerIndex: 0,
            FandomId: "cortis",
            Artist: "Juhoon",
            ProductName: "CORTIS Juhoon Weverse Shop Limited Photocard",
            Type: "Photocard",
            Condition: ProductCondition.New,
            Description: "Photocard giới hạn của Juhoon (CORTIS) từ Weverse Shop, phiên bản exclusive không bán đại trà. Card còn nguyên sleeve bảo vệ, chưa qua sử dụng. Cực hiếm, dành cho fan CORTIS chính hiệu.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636963/Cortis_Juhoon_weverse_SHOP_Limited_Photocard_rfe1f0.jpg",
            FloorPrice: 150_000m,
            ReservePrice: 300_000m,
            DurationHours: 24,
            BidHistory: new[]
            {
                (BidderIndex: 2, Amount: 150_000m, MinutesAgo: 180),
                (BidderIndex: 4, Amount: 180_000m, MinutesAgo: 120),
                (BidderIndex: 2, Amount: 210_000m, MinutesAgo: 60),
                (BidderIndex: 1, Amount: 240_000m, MinutesAgo: 20)
            }
        ),
        new(
            SellerIndex: 1,
            FandomId: "bts",
            Artist: "BTS",
            ProductName: "BTS The Best Limited Edition Type C",
            Type: "Album",
            Condition: ProductCondition.LikeNew,
            Description: "Album compilation Nhật Bản BTS The Best phiên bản Limited Edition Type C. Bao gồm 2 CD, Blu-ray, photobook 100 trang và photocard. Đã bóc seal nhưng chưa nghe, tình trạng như mới. Phiên bản giới hạn chỉ phát hành tại Nhật.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636964/BTS_THE_BEST_Limited_Edition_C_yavpbm.jpg",
            FloorPrice: 650_000m,
            ReservePrice: 950_000m,
            DurationHours: 72,
            BidHistory: new[]
            {
                (BidderIndex: 3, Amount: 650_000m, MinutesAgo: 120),
                (BidderIndex: 0, Amount: 700_000m, MinutesAgo: 45)
            }
        ),
        new(
            SellerIndex: 2,
            FandomId: "blackpink",
            Artist: "BLACKPINK",
            ProductName: "BLACKPINK x Takashi Murakami Pandakashi Limited",
            Type: "Merch",
            Condition: ProductCondition.New,
            Description: "Poster collab giới hạn giữa BLACKPINK và nghệ sĩ Takashi Murakami, phát hành qua NTWRK. Thiết kế nhân vật Pandakashi độc đáo kết hợp logo BP. Hàng limited edition chính hãng, còn nguyên ống đựng, chưa mở.",
            PrimaryImageUrl: "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636964/BLACKPINK_Takashi_Murakami_Pandakashi_LIMITED_npwztd.jpg",
            FloorPrice: 500_000m,
            ReservePrice: 900_000m,
            DurationHours: 60,
            BidHistory: new[]
            {
                (BidderIndex: 3, Amount: 500_000m, MinutesAgo: 300),
                (BidderIndex: 0, Amount: 560_000m, MinutesAgo: 220),
                (BidderIndex: 4, Amount: 620_000m, MinutesAgo: 140),
                (BidderIndex: 3, Amount: 680_000m, MinutesAgo: 70),
                (BidderIndex: 0, Amount: 740_000m, MinutesAgo: 15)
            }
        )
    };

    public static async Task SeedAsync(EzBiasDbContext db, IReadOnlyList<User> sellers, CancellationToken ct = default)
    {
        if (!db.Fandoms.Any(x => x.Id == "straykids"))
        {
            FandomNameNormalizer.TryNormalize("Stray Kids", out _, out var normalizedName, out _);
            db.Fandoms.Add(new Fandom
            {
                Id = "straykids",
                Name = "Stray Kids",
                NormalizedName = normalizedName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        var bidders = await EnsureBiddersAsync(db, ct);

        var now = DateTimeOffset.UtcNow;

        var existingAuctionImageUrls = db.Products
            .Where(x => x.IsAuction)
            .Select(x => x.PrimaryImageUrl)
            .ToHashSet();

        foreach (var entry in Entries)
        {
            Product product;

            if (existingAuctionImageUrls.Contains(entry.PrimaryImageUrl))
            {
                product = db.Products.FirstOrDefault(x => x.IsAuction && x.PrimaryImageUrl == entry.PrimaryImageUrl)!;
                if (product is null) continue;
            }
            else
            {
                product = new Product
                {
                    SellerId = sellers[entry.SellerIndex % sellers.Count].Id,
                    FandomId = entry.FandomId,
                    Artist = entry.Artist,
                    Name = entry.ProductName,
                    Type = entry.Type,
                    Condition = entry.Condition,
                    Price = entry.FloorPrice,
                    Stock = 1,
                    Description = entry.Description,
                    PrimaryImageUrl = entry.PrimaryImageUrl,
                    IsAuction = true,
                    Status = ProductStatus.Active,
                    CreatedAt = now,
                    Images = new List<ProductImage>
                    {
                        new() { Url = entry.PrimaryImageUrl, SortOrder = 1, CreatedAt = now }
                    }
                };

                db.Products.Add(product);
                await db.SaveChangesAsync(ct);

                var currentBid = entry.BidHistory.Length > 0
                    ? entry.BidHistory.Max(b => b.Amount)
                    : 0m;

                var auction = new Auction
                {
                    ProductId = product.Id,
                    SellerId = sellers[entry.SellerIndex % sellers.Count].Id,
                    FloorPrice = entry.FloorPrice,
                    ReservePrice = entry.ReservePrice,
                    CurrentBid = currentBid,
                    ExtensionSeconds = 300,
                    TriggerBeforeEnd = 60,
                    Status = AuctionStatus.Live,
                    EndsAt = now.AddHours(entry.DurationHours),
                    CreatedAt = now
                };

                db.Auctions.Add(auction);
                await db.SaveChangesAsync(ct);
            }

            if (entry.BidHistory.Length == 0) continue;

            var existingAuction = db.Auctions.FirstOrDefault(x => x.ProductId == product.Id);
            if (existingAuction is null) continue;

            var existingBidAmounts = db.Bids
                .Where(x => x.AuctionId == existingAuction.Id)
                .Select(x => x.Amount)
                .ToHashSet();

            var sortedBids = entry.BidHistory.OrderBy(b => b.MinutesAgo).ToList();
            var maxAmount = entry.BidHistory.Max(b => b.Amount);
            var addedAny = false;

            var existingWinningBids = db.Bids
                .Where(x => x.AuctionId == existingAuction.Id && x.IsWinning)
                .ToList();
            foreach (var wb in existingWinningBids)
                wb.IsWinning = false;

            for (var i = 0; i < sortedBids.Count; i++)
            {
                var (bidderIndex, amount, minutesAgo) = sortedBids[i];
                if (existingBidAmounts.Contains(amount)) continue;

                var bidder = bidders[bidderIndex % bidders.Count];

                db.Bids.Add(new Bid
                {
                    AuctionId = existingAuction.Id,
                    UserId = bidder.Id,
                    Amount = amount,
                    IsWinning = amount == maxAmount,
                    PlacedAt = now.AddMinutes(-minutesAgo)
                });
                addedAny = true;
            }

            var allBidsForAuction = db.Bids
                .Where(x => x.AuctionId == existingAuction.Id)
                .ToList();
            var topBid = allBidsForAuction.MaxBy(x => x.Amount);
            if (topBid is not null)
                topBid.IsWinning = true;

            if (addedAny)
            {
                var maxBid = entry.BidHistory.Max(b => b.Amount);
                if (existingAuction.CurrentBid < maxBid)
                    existingAuction.CurrentBid = maxBid;

                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task<List<User>> EnsureBiddersAsync(EzBiasDbContext db, CancellationToken ct)
    {
        var users = new List<User>();

        foreach (var (fullName, username, email) in BidderSeeds)
        {
            var existing = db.Users.FirstOrDefault(x => x.Email == email);
            if (existing is null)
            {
                existing = new User
                {
                    FullName = fullName,
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Bidder@123"),
                    Role = UserRole.User,
                    EmailVerifiedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Users.Add(existing);
                await db.SaveChangesAsync(ct);
            }

            users.Add(existing);
        }

        return users;
    }

    private record AuctionSeedEntry(
        int SellerIndex,
        string FandomId,
        string Artist,
        string ProductName,
        string Type,
        ProductCondition Condition,
        string Description,
        string PrimaryImageUrl,
        decimal FloorPrice,
        decimal? ReservePrice,
        int DurationHours,
        (int BidderIndex, decimal Amount, int MinutesAgo)[] BidHistory
    );
}
