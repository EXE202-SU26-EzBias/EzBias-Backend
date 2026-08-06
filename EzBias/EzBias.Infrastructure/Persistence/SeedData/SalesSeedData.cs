using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Infrastructure.Persistence.SeedData;

public static class SalesSeedData
{
    private const decimal CommissionRatePercent = 8m;
    private const string MarkerBuyerEmail = "buyer1.sales@ezbias.local";

    private static readonly Random Rng = new(20260605);

    public static async Task SeedAsync(EzBiasDbContext db, CancellationToken ct = default)
    {
        if (db.Users.Any(u => u.Email == MarkerBuyerEmail))
            return;

        var now = DateTimeOffset.UtcNow;

        BackdateExistingSeedUsers(db, now);

        var sellers = await EnsureFiveSellersAsync(db, now, ct);

        var buyers = await CreateBuyersAsync(db, now, ct);

        var productsBySeller = await CreateSaleProductsAsync(db, sellers, now, ct);

        await GenerateOrdersAsync(db, sellers, buyers, productsBySeller, now, ct);
    }


    private static void BackdateExistingSeedUsers(EzBiasDbContext db, DateTimeOffset now)
    {
        var demoUsers = db.Users
            .Where(u => u.Email.EndsWith("@ezbias.local"))
            .ToList();

        var changed = false;
        var offset = 30;
        foreach (var u in demoUsers.OrderBy(u => u.Id))
        {
            if (u.CreatedAt < now.AddDays(-2)) continue;
            u.CreatedAt = now.AddDays(-offset);
            offset += 22;
            if (offset > 300) offset = 35;
            changed = true;
        }

        if (changed) db.SaveChanges();
    }


    private static readonly (string Bank, string AccountNo)[] SellerBanks =
    {
        ("Vietcombank",  "0011000123456"),
        ("Techcombank",  "1903 5550 1234"),
        ("MB Bank",      "0987654321098"),
        ("ACB",          "2468013579246"),
        ("BIDV",         "3141592653589"),
    };

    private static async Task<List<User>> EnsureFiveSellersAsync(EzBiasDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var sellers = ProductSeedData.GetSeedSellers(db);

        var extra = new (string FullName, string Username, string Email)[]
        {
            ("Demo Seller 4", "demo_seller_4", "seller4.demo@ezbias.local"),
            ("Demo Seller 5", "demo_seller_5", "seller5.demo@ezbias.local"),
        };

        foreach (var (fullName, username, email) in extra)
        {
            var existing = db.Users.FirstOrDefault(x => x.Email == email);
            if (existing is null)
            {
                existing = new User
                {
                    FullName = fullName,
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Seller@123"),
                    Role = UserRole.User,
                    EmailVerifiedAt = now,
                    CreatedAt = now.AddDays(-200)
                };
                db.Users.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            sellers.Add(existing);
        }

        for (var i = 0; i < sellers.Count && i < SellerBanks.Length; i++)
        {
            var s = sellers[i];
            if (string.IsNullOrWhiteSpace(s.BankName))
            {
                s.BankName = SellerBanks[i].Bank;
                s.BankAccountNumber = SellerBanks[i].AccountNo;
                s.BankAccountName = s.FullName.ToUpperInvariant();
                s.UpdatedAt = now;
            }
        }
        await db.SaveChangesAsync(ct);

        return sellers.Take(5).ToList();
    }


    private const int BuyerCount = 43;
    private const int BuyersCreatedToday = 9;

    private static readonly string[] Ho = { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô", "Dương", "Lý" };
    private static readonly string[] Ten = { "An", "Bình", "Châu", "Dũng", "Hà", "Hương", "Khoa", "Lan", "Mai", "Nam", "Oanh", "Phúc", "Quân", "Sơn", "Trang", "Uyên", "Vy", "Yến", "Linh", "Tú", "Khánh", "Ngọc" };
    private static readonly string[] Cities = { "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ", "Huế", "Nha Trang", "Biên Hòa" };

    private static async Task<List<User>> CreateBuyersAsync(EzBiasDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Buyer@123");
        var buyers = new List<User>(BuyerCount);
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < BuyerCount; i++)
        {
            var fullName = $"{Ho[i % Ho.Length]} {Ten[(i * 7 + 3) % Ten.Length]}";

            DateTimeOffset createdAt;
            if (i < BuyersCreatedToday)
            {
                var span = now - todayStart - TimeSpan.FromMinutes(20);
                var secs = span.TotalSeconds > 0 ? Rng.Next(1800, (int)span.TotalSeconds) : 600;
                createdAt = todayStart.AddSeconds(secs);
            }
            else
            {
                createdAt = now.AddDays(-Rng.Next(3, 330)).AddHours(-Rng.Next(0, 24));
            }

            buyers.Add(new User
            {
                FullName = fullName,
                Username = $"buyer_{i + 1}_sales",
                Email = $"buyer{i + 1}.sales@ezbias.local",
                PasswordHash = passwordHash,
                Role = UserRole.User,
                City = Cities[i % Cities.Length],
                EmailVerifiedAt = createdAt,
                CreatedAt = createdAt
            });
        }

        db.Users.AddRange(buyers);
        await db.SaveChangesAsync(ct);
        return buyers;
    }


    private record SaleProductTemplate(int SellerIndex, string FandomId, string Artist, string Name, string Type, ProductCondition Condition, decimal Price, string PrimaryImageUrl = "");

    private static readonly SaleProductTemplate[] ProductTemplates =
    {
        new(0, "twice",     "TWICE",            "TWICE Ready To Be Tour T-Shirt", "Apparel",   ProductCondition.LikeNew, 350_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636016/TWICE_Ready_To_Be_Tour_T-Shirt_r7ub1d.jpg"),
        new(0, "straykids", "Stray Kids",       "Stray Kids SKZOO Plush",         "Merch",     ProductCondition.New,     280_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636015/Stray_Kids_SKZOO_Plush_vfbmjv.jpg"),
        new(0, "newjeans",  "NewJeans",         "NewJeans Get Up Photocard",      "Photocard", ProductCondition.New,     160_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636016/NewJeans_OMG_Photocard_Set_rhacx9.jpg"),
        new(0, "bts",       "BTS",              "BTS Proof Collector Edition",    "Album",     ProductCondition.LikeNew, 520_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636015/BTS_Butter_Album_Peaches_Version_kqm2w6.jpg"),

        new(1, "cortis",    "Juhoon",           "CORTIS Juhoon Photocard",        "Photocard", ProductCondition.New,     150_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636963/Cortis_Juhoon_weverse_SHOP_Limited_Photocard_rfe1f0.jpg"),
        new(1, "blackpink", "BLACKPINK",        "BLACKPINK Born Pink Lightstick", "Merch",     ProductCondition.Good,    600_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636015/BLACKPINK_Lightstick_Ver.2_doxxrs.jpg"),
        new(1, "twice",     "TWICE",            "TWICE With YOU-th Album",        "Album",     ProductCondition.New,     300_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636016/TWICE_Ready_To_Be_Tour_T-Shirt_r7ub1d.jpg"),
        new(1, "snsd",      "Girls' Generation","SNSD FOREVER 1 Photobook",       "Merch",     ProductCondition.LikeNew, 240_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526485/Mini_Light_SNSD_Girls_Generation_zdrhtp.jpg"),

        new(2, "bts",       "BTS",              "BTS Dynamite Photocard Set",     "Photocard", ProductCondition.New,     190_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526490/Card_%E1%BA%A3nh_Lotte_Layover_V_BTS_i0ivpu.jpg"),
        new(2, "blackpink", "Jisoo",            "BLACKPINK Jisoo Me Single",      "Album",     ProductCondition.New,     350_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526484/BLACKPINK_3rd_Mini_Album_Deadline_Silver_Version_gdbb5k.jpg"),
        new(2, "newjeans",  "NewJeans",         "NewJeans Bunny Beach Bag",       "Merch",     ProductCondition.New,     280_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526485/LOMO_CARD_BO_G%C3%93C_CORTIS_lpgvse.jpg"),

        new(3, "cortis",    "CORTIS",           "CORTIS Color Outside Poster",    "Merch",     ProductCondition.New,     120_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526486/Pre-order_CORTIS_The_1st_EP_COLOR_OUTSIDE_THE_LINES_u2o7pj.jpg"),
        new(3, "bts",       "Jungkook",         "BTS Jungkook Golden Album",      "Album",     ProductCondition.New,     380_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779636964/BTS_THE_BEST_Limited_Edition_C_yavpbm.jpg"),
        new(3, "twice",     "Nayeon",           "TWICE Nayeon IM NAYEON PC",      "Photocard", ProductCondition.New,     170_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526486/10_card_gi%E1%BA%A5y_bo_g%C3%B3c_Jennie_ug9zax.jpg"),

        new(4, "blackpink", "Lisa",             "BLACKPINK Lisa Solo Photocard",  "Photocard", ProductCondition.New,     200_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526487/Polaroid_BlackpinK_fkrgrv.jpg"),
        new(4, "straykids", "Stray Kids",       "Stray Kids 5-STAR Album",        "Album",     ProductCondition.New,     320_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526486/WTS_BTS_MERCH_BOX_3_x6dfbn.jpg"),
        new(4, "snsd",      "Girls' Generation","SNSD Mini Light Keyring",        "Merch",     ProductCondition.New,     130_000m, "https://res.cloudinary.com/db7ueln9w/image/upload/v1779526485/Mini_Light_SNSD_Girls_Generation_zdrhtp.jpg"),
    };

    private static async Task<Dictionary<long, List<Product>>> CreateSaleProductsAsync(EzBiasDbContext db, List<User> sellers, DateTimeOffset now, CancellationToken ct)
    {
        var products = new List<Product>(ProductTemplates.Length);

        foreach (var t in ProductTemplates)
        {
            var seller = sellers[t.SellerIndex % sellers.Count];
            products.Add(new Product
            {
                SellerId = seller.Id,
                FandomId = t.FandomId,
                Artist = t.Artist,
                Name = t.Name,
                Type = t.Type,
                Condition = t.Condition,
                Price = t.Price,
                Stock = Rng.Next(8, 40),
                Description = $"{t.Name} — chính hãng, đóng gói cẩn thận. Hàng seed phục vụ demo dashboard.",
                PrimaryImageUrl = t.PrimaryImageUrl,
                IsAuction = false,
                Status = ProductStatus.Active,
                CreatedAt = now.AddDays(-Rng.Next(120, 360))
            });
        }

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        return products
            .GroupBy(p => p.SellerId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }


    private const int OrderCount = 165;

    private static readonly int[] SellerWeights = { 32, 26, 20, 14, 8 };

    private static readonly int[] MonthWeights = { 6, 9, 7, 12, 8, 14, 10, 6, 13, 9, 15, 11 };

    private static readonly (OrderStatus Status, int Weight)[] StatusWeights =
    {
        (OrderStatus.Completed,       42),
        (OrderStatus.Delivered,       14),
        (OrderStatus.Shipped,          9),
        (OrderStatus.Processing,       7),
        (OrderStatus.Paid,             7),
        (OrderStatus.Pending,          6),
        (OrderStatus.Canceled,         8),
        (OrderStatus.Refunded,         4),
        (OrderStatus.ReturnRequested,  3),
    };

    private static async Task GenerateOrdersAsync(
        EzBiasDbContext db,
        List<User> sellers,
        List<User> buyers,
        Dictionary<long, List<Product>> productsBySeller,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var sellerTotalWeight = SellerWeights.Take(sellers.Count).Sum();
        var monthTotalWeight = MonthWeights.Sum();
        var statusTotalWeight = StatusWeights.Sum(s => s.Weight);

        var orders = new List<Order>(OrderCount);
        var seq = 0;
        var disputeCreated = 0;

        for (var n = 0; n < OrderCount; n++)
        {
            seq++;
            var sellerIdx = WeightedPick(SellerWeights.Take(sellers.Count).ToArray(), sellerTotalWeight);
            var seller = sellers[sellerIdx];
            var sellerProducts = productsBySeller[seller.Id];
            var buyer = buyers[Rng.Next(buyers.Count)];

            var monthsBack = 11 - WeightedPick(MonthWeights, monthTotalWeight);
            var createdAt = RandomInstantInMonth(now, monthsBack);
            var status = StatusWeights[WeightedPickIndex(StatusWeights.Select(s => s.Weight).ToArray(), statusTotalWeight)].Status;

            var itemCount = Rng.Next(1, Math.Min(4, sellerProducts.Count + 1));
            var chosen = sellerProducts.OrderBy(_ => Rng.Next()).Take(itemCount).ToList();
            var items = new List<OrderItem>(chosen.Count);
            decimal total = 0;
            foreach (var p in chosen)
            {
                var qty = Rng.Next(1, 3);
                var subtotal = p.Price * qty;
                total += subtotal;
                items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductImage = p.PrimaryImageUrl,
                    Quantity = qty,
                    UnitPrice = p.Price,
                    Subtotal = subtotal
                });
            }

            var order = new Order
            {
                UserId = buyer.Id,
                SellerId = seller.Id,
                Source = OrderSource.Cart,
                Total = total,
                Status = status,
                AddressSnap = BuildAddressSnap(buyer),
                CreatedAt = createdAt,
                Items = items
            };

            ApplyStatusGraph(order, status, buyer, seller, createdAt, now, seq, ref disputeCreated);
            orders.Add(order);
        }

        const int chunk = 40;
        for (var i = 0; i < orders.Count; i += chunk)
        {
            db.Orders.AddRange(orders.Skip(i).Take(chunk));
            await db.SaveChangesAsync(ct);
        }

        foreach (var order in orders)
        {
            foreach (var refund in order.Refunds)
            {
                if (refund.Status != RefundStatus.Completed || refund.ProviderRef is not null)
                    continue;

                var ts = refund.ProcessedAt ?? refund.CreatedAt;
                refund.ProviderRef = $"REF-DSP-{ts:yyyyMMddHHmmss}-{order.Id}";
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static void ApplyStatusGraph(
        Order order,
        OrderStatus status,
        User buyer,
        User seller,
        DateTimeOffset createdAt,
        DateTimeOffset now,
        int seq,
        ref int disputeCreated)
    {
        DateTimeOffset Clamp(DateTimeOffset d) => d > now ? now : d;

        var reference = $"PAY-{createdAt:yyyyMMddHHmmss}-{seq}";

        if (status == OrderStatus.Pending)
        {
            order.PaymentOrders.Add(new PaymentOrder
            {
                Payment = new Payment
                {
                    UserId = buyer.Id,
                    Type = PaymentType.Order,
                    Amount = order.Total,
                    Status = PaymentStatus.Pending,
                    Reference = reference,
                    CreatedAt = createdAt
                }
            });
            return;
        }

        if (status == OrderStatus.Canceled)
        {
            order.PaymentOrders.Add(new PaymentOrder
            {
                Payment = new Payment
                {
                    UserId = buyer.Id,
                    Type = PaymentType.Order,
                    Amount = order.Total,
                    Status = PaymentStatus.Failed,
                    Reference = reference,
                    CreatedAt = createdAt
                }
            });
            return;
        }

        var paidAt = Clamp(createdAt.AddMinutes(8));

        var payment = new Payment
        {
            UserId = buyer.Id,
            Type = PaymentType.Order,
            Amount = order.Total,
            Status = PaymentStatus.Paid,
            Reference = reference,
            CreatedAt = createdAt,
            PaidAt = paidAt
        };
        order.PaymentOrders.Add(new PaymentOrder { Payment = payment });

        if (status == OrderStatus.Refunded)
        {
            var refundedAt = Clamp(createdAt.AddDays(Rng.Next(2, 8)));
            order.Refunds.Add(new Refund
            {
                Payment = payment,
                Amount = order.Total,
                Reason = "Buyer returned item — full refund.",
                Status = RefundStatus.Completed,
                ProcessedAt = refundedAt,
                CreatedAt = Clamp(createdAt.AddDays(1))
            });
            return;
        }

        var commissionAmount = Math.Round(order.Total * CommissionRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        var sellerNet = order.Total - commissionAmount;

        order.CommissionTransaction = new CommissionTransaction
        {
            Payment = payment,
            SellerId = seller.Id,
            GrossAmount = order.Total,
            CommissionRatePercent = CommissionRatePercent,
            CommissionAmount = commissionAmount,
            SellerNetAmount = sellerNet,
            Currency = "VND",
            CreatedAt = paidAt
        };

        order.EscrowTransactions.Add(new EscrowTransaction
        {
            SellerId = seller.Id,
            Type = EscrowType.IN,
            Amount = order.Total,
            Payment = payment,
            CreatedAt = paidAt
        });

        if (status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Completed or OrderStatus.ReturnRequested)
            order.ShippedAt = Clamp(createdAt.AddDays(1));

        if (status is OrderStatus.Delivered or OrderStatus.Completed or OrderStatus.ReturnRequested)
            order.DeliveredAt = Clamp(createdAt.AddDays(4));

        if (status == OrderStatus.Completed)
        {
            order.CompletedAt = Clamp(createdAt.AddDays(7));

            var payoutRoll = Rng.NextDouble();
            var payoutStatus = payoutRoll < 0.6 ? PayoutStatus.Approved
                : payoutRoll < 0.9 ? PayoutStatus.Pending
                : PayoutStatus.Rejected;
            var payoutApproved = payoutStatus == PayoutStatus.Approved;
            var payout = new Payout
            {
                SellerId = seller.Id,
                Amount = sellerNet,
                Status = payoutStatus,
                CreatedAt = Clamp(createdAt.AddDays(7)),
                BankTransferRef = payoutApproved ? $"PO-{createdAt:yyyyMMddHHmmss}-{seq}" : null,
                PaidAt = payoutApproved ? Clamp(createdAt.AddDays(8)) : null
            };
            order.Payout = payout;

            if (payoutApproved)
            {
                order.EscrowTransactions.Add(new EscrowTransaction
                {
                    SellerId = seller.Id,
                    Type = EscrowType.OUT,
                    Amount = sellerNet,
                    Payout = payout,
                    CreatedAt = payout.PaidAt!.Value
                });
            }

        }

        if (status == OrderStatus.ReturnRequested && disputeCreated < 4)
        {
            disputeCreated++;
            var dispute = new Dispute
            {
                InitiatorId = buyer.Id,
                Reason = "Item not as described — requesting return.",
                Status = disputeCreated % 2 == 0 ? DisputeStatus.UnderReview : DisputeStatus.Open,
                CreatedAt = Clamp(createdAt.AddDays(5))
            };
            order.Dispute = dispute;
            order.Refunds.Add(new Refund
            {
                Payment = payment,
                Dispute = dispute,
                Amount = order.Total,
                Reason = "Pending dispute resolution.",
                Status = RefundStatus.Pending,
                CreatedAt = Clamp(createdAt.AddDays(5))
            });
        }
    }


    private static string BuildAddressSnap(User buyer)
        => $"{{\"fullName\":\"{buyer.FullName}\",\"city\":\"{buyer.City}\",\"line\":\"123 Demo Street\"}}";

    private static DateTimeOffset RandomInstantInMonth(DateTimeOffset now, int monthsBack)
    {
        var target = now.AddMonths(-monthsBack);
        var year = target.Year;
        var month = target.Month;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var maxDay = monthsBack == 0 ? Math.Min(daysInMonth, now.Day) : daysInMonth;
        if (maxDay < 1) maxDay = 1;

        var day = Rng.Next(1, maxDay + 1);
        var dt = new DateTimeOffset(year, month, day, Rng.Next(8, 22), Rng.Next(0, 60), Rng.Next(0, 60), TimeSpan.Zero);
        return dt > now ? now.AddHours(-1) : dt;
    }

    private static int WeightedPickIndex(int[] weights, int totalWeight)
    {
        var roll = Rng.Next(totalWeight);
        var cumulative = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return i;
        }
        return weights.Length - 1;
    }

    private static int WeightedPick(int[] weights, int totalWeight) => WeightedPickIndex(weights, totalWeight);
}
