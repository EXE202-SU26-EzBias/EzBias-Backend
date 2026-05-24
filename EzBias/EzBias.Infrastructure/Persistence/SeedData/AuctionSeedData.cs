using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Infrastructure.Persistence.SeedData;

public static class AuctionSeedData
{
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
            DurationHours: 24
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
            DurationHours: 48
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
            DurationHours: 36
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
            DurationHours: 72
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
            DurationHours: 48
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
            DurationHours: 48
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
            DurationHours: 24
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
            DurationHours: 72
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
            DurationHours: 60
        )
    };

    public static async Task SeedAsync(EzBiasDbContext db, IReadOnlyList<User> sellers, CancellationToken ct = default)
    {
        // Ensure straykids fandom exists
        if (!db.Fandoms.Any(x => x.Id == "straykids"))
        {
            db.Fandoms.Add(new Fandom { Id = "straykids", Name = "Stray Kids", IsActive = true, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        var now = DateTimeOffset.UtcNow;

        // Use PrimaryImageUrl as unique key to avoid duplicate seeding
        var existingAuctionImageUrls = db.Products
            .Where(x => x.IsAuction)
            .Select(x => x.PrimaryImageUrl)
            .ToHashSet();

        foreach (var entry in Entries)
        {
            if (existingAuctionImageUrls.Contains(entry.PrimaryImageUrl))
                continue;

            var seller = sellers[entry.SellerIndex % sellers.Count];

            var product = new Product
            {
                SellerId = seller.Id,
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

            var auction = new Auction
            {
                ProductId = product.Id,
                SellerId = seller.Id,
                FloorPrice = entry.FloorPrice,
                ReservePrice = entry.ReservePrice,
                CurrentBid = 0m,
                ExtensionSeconds = 300,
                TriggerBeforeEnd = 60,
                Status = AuctionStatus.Live,
                EndsAt = now.AddHours(entry.DurationHours),
                CreatedAt = now
            };

            db.Auctions.Add(auction);
            await db.SaveChangesAsync(ct);
        }
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
        int DurationHours
    );
}
