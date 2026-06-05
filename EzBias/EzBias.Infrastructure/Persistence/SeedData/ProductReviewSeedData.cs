using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds the <c>product_reviews</c> table so every product detail page shows star ratings
/// and written comments in the UI.
///
/// Two flavours of review are produced:
///  - <b>Purchase-backed</b>: where a demo buyer has a Delivered/Completed order containing the
///    product, the review is attributed to that buyer — so logging in as them shows the editable
///    form (the same purchase gate as <c>IOrderRepository.HasUserPurchasedProductAsync</c>).
///  - <b>Display-only</b>: products without qualifying orders (most hero catalog items) still get
///    reviews from the buyer pool so their pages aren't empty.
///
/// Tone is mostly positive with a few critical reviews (avg ~4.2). Idempotent: a no-op once any
/// review exists. Runs AFTER SalesSeedData (relies on the *.sales@ezbias.local buyer pool).
/// </summary>
public static class ProductReviewSeedData
{
    private static readonly Random Rng = new(20260606);

    private const int MinReviewsPerProduct = 2;
    private const int MaxReviewsPerProduct = 6;
    private const double NullCommentChance = 0.15;
    private const double EditedChance = 0.10;

    public static async Task SeedAsync(EzBiasDbContext db, CancellationToken ct = default)
    {
        // Idempotency — only seed an empty table.
        if (db.ProductReviews.Any())
            return;

        // Buyer pool created by SalesSeedData. If it's missing, SalesSeedData hasn't run yet.
        var buyers = db.Users
            .Where(u => u.Email.EndsWith(".sales@ezbias.local"))
            .OrderBy(u => u.Id)
            .ToList();
        if (buyers.Count == 0)
            return;

        var products = db.Products
            .Where(p => p.DeletedAt == null && p.Status != ProductStatus.Archived)
            .OrderBy(p => p.Id)
            .ToList();
        if (products.Count == 0)
            return;

        // Genuine purchasers per product (Delivered/Completed orders) — same whitelist as the gate.
        var purchaseMap = BuildPurchaseMap(db);

        var now = DateTimeOffset.UtcNow;
        var reviews = new List<ProductReview>();

        foreach (var product in products)
        {
            var target = Rng.Next(MinReviewsPerProduct, MaxReviewsPerProduct + 1);
            var used = new HashSet<long>();

            // 1. Purchase-backed reviewers first (shuffled, never the seller).
            var purchasers = purchaseMap.TryGetValue(product.Id, out var ids)
                ? ids.Where(id => id != product.SellerId).OrderBy(_ => Rng.Next()).ToList()
                : new List<long>();

            foreach (var userId in purchasers)
            {
                if (used.Count >= target) break;
                if (!used.Add(userId)) continue;
                reviews.Add(BuildReview(product.Id, userId, now));
            }

            // 2. Fill the remainder from the broader buyer pool (display-only).
            var fillerPool = buyers
                .Where(b => b.Id != product.SellerId && !used.Contains(b.Id))
                .OrderBy(_ => Rng.Next())
                .ToList();

            foreach (var buyer in fillerPool)
            {
                if (used.Count >= target) break;
                if (!used.Add(buyer.Id)) continue;
                reviews.Add(BuildReview(product.Id, buyer.Id, now));
            }
        }

        // Chunked insert to keep the change-tracker light.
        const int chunk = 40;
        for (var i = 0; i < reviews.Count; i += chunk)
        {
            db.ProductReviews.AddRange(reviews.Skip(i).Take(chunk));
            await db.SaveChangesAsync(ct);
        }
    }

    private static Dictionary<long, List<long>> BuildPurchaseMap(EzBiasDbContext db)
    {
        var pairs = db.Orders
            .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed)
            .SelectMany(o => o.Items
                .Where(i => i.ProductId != null)
                .Select(i => new { ProductId = i.ProductId!.Value, o.UserId }))
            .Distinct()
            .ToList();

        return pairs
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().ToList());
    }

    private static ProductReview BuildReview(long productId, long userId, DateTimeOffset now)
    {
        var stars = PickStars();
        var createdAt = now.AddDays(-Rng.Next(1, 120)).AddHours(-Rng.Next(0, 24));

        DateTimeOffset? updatedAt = null;
        if (Rng.NextDouble() < EditedChance)
        {
            var edited = createdAt.AddDays(Rng.Next(1, 6));
            updatedAt = edited > now ? now : edited;
        }

        var comment = Rng.NextDouble() < NullCommentChance ? null : PickComment(stars);

        return new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Stars = stars,
            Comment = comment,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    // Weighted toward 4-5 stars; avg ≈ 4.2.
    private static readonly (short Stars, int Weight)[] StarWeights =
    {
        (5, 50),
        (4, 30),
        (3, 12),
        (2, 5),
        (1, 3),
    };

    private static short PickStars()
    {
        var total = StarWeights.Sum(s => s.Weight);
        var roll = Rng.Next(total);
        var cumulative = 0;
        foreach (var (stars, weight) in StarWeights)
        {
            cumulative += weight;
            if (roll < cumulative) return stars;
        }
        return 5;
    }

    private static readonly string[] FiveStar =
    {
        "Hàng đẹp y hình, đóng gói cực kỹ. Sẽ ủng hộ shop tiếp!",
        "Card chính hãng, sắc nét, giao nhanh. Quá hài lòng.",
        "Seller uy tín, bọc chống cong cẩn thận. 10 điểm!",
        "Đẹp hơn mong đợi, đúng mô tả. Cảm ơn shop nhiều.",
        "Chất lượng tuyệt vời, giá hợp lý. Recommend!",
    };

    private static readonly string[] FourStar =
    {
        "Hàng đẹp, đóng gói ổn. Giao hơi chậm một chút.",
        "Ưng ý, chỉ tiếc góc bọc thêm thì hoàn hảo.",
        "Chất lượng tốt, đúng hình. Sẽ mua lại.",
        "Khá hài lòng, ship nhanh nhưng hộp hơi móp nhẹ.",
    };

    private static readonly string[] ThreeStar =
    {
        "Tạm được, có vài vết xước nhẹ ở góc.",
        "Bình thường so với giá, đóng gói đơn giản.",
        "Hàng ổn nhưng màu hơi khác ảnh một chút.",
    };

    private static readonly string[] LowStar =
    {
        "Card bị cong, bọc không kỹ. Hơi thất vọng.",
        "Không giống ảnh lắm, giao trễ mấy ngày.",
        "Chất lượng dưới mong đợi, góc bị móp.",
    };

    private static string PickComment(short stars) => stars switch
    {
        5 => FiveStar[Rng.Next(FiveStar.Length)],
        4 => FourStar[Rng.Next(FourStar.Length)],
        3 => ThreeStar[Rng.Next(ThreeStar.Length)],
        _ => LowStar[Rng.Next(LowStar.Length)],
    };
}
