using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds realistic AuctionDeposit (cọc đấu giá) and Refund (hoàn tiền) records so the
/// Admin → Orders / Transaction History tab has data for all 4 transaction types.
///
/// Deposits: some Held (active), some Applied (winner paid), some Refunded (losers refunded).
/// Refunds:  Pending ones (approved but not yet transferred) and Completed ones (with ProviderRef).
///
/// Idempotent: guarded by a marker payment reference, runs once.
/// Runs AFTER AuctionSeedData (needs auctions) and SalesSeedData (needs buyers).
/// </summary>
public static class TransactionSeedData
{
    private static readonly Random Rng = new(20260612);

    public static async Task SeedAsync(EzBiasDbContext db, CancellationToken ct = default)
    {
        // Idempotency guard — if any seeded deposits already exist, do nothing.
        if (db.AuctionDeposits.Any())
            return;

        var now = DateTimeOffset.UtcNow;

        // Pick buyers from sales seed pool
        var buyers = db.Users
            .Where(u => u.Email.EndsWith(".sales@ezbias.local"))
            .OrderBy(u => u.Id)
            .Take(20)
            .ToList();

        if (buyers.Count == 0) return;

        // Pick auctions that are live
        var auctions = db.Auctions
            .Where(a => a.Status == AuctionStatus.Live)
            .Take(6)
            .ToList();

        if (auctions.Count == 0) return;

        // Pick orders that are Completed for Refund seed
        var completedOrders = db.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .Take(10)
            .ToList();

        // ----------------------------------------------------------------
        // 1. Auction Deposits
        // ----------------------------------------------------------------
        var depositSeeds = new List<(User Buyer, Auction Auction, DepositState State, int DaysAgo)>();

        for (var i = 0; i < Math.Min(auctions.Count, buyers.Count); i++)
        {
            var auction = auctions[i % auctions.Count];
            var buyer = buyers[i % buyers.Count];

            // Vary states: Held, Applied, Refunded
            var state = i % 3 == 0 ? DepositState.Held
                      : i % 3 == 1 ? DepositState.Applied
                      : DepositState.Refunded;

            depositSeeds.Add((buyer, auction, state, DaysAgo: Rng.Next(1, 15)));
        }

        // Add a few extra Held deposits from different buyers
        for (var i = 0; i < 4; i++)
        {
            var auction = auctions[i % auctions.Count];
            var buyer = buyers[(i + 5) % buyers.Count];
            depositSeeds.Add((buyer, auction, DepositState.Held, DaysAgo: Rng.Next(1, 7)));
        }

        foreach (var (buyer, auction, state, daysAgo) in depositSeeds)
        {
            var depositAmount = Math.Round(auction.FloorPrice * 0.1m, 0);
            var createdAt = now.AddDays(-daysAgo).AddHours(-Rng.Next(0, 12));
            var paidAt = createdAt.AddMinutes(Rng.Next(3, 30));
            var seq = Rng.Next(100000, 999999);

            var payment = new Payment
            {
                UserId = buyer.Id,
                Type = PaymentType.AuctionDeposit,
                Amount = depositAmount,
                Currency = "VND",
                Status = PaymentStatus.Paid,
                Reference = $"PAY-{createdAt:yyyyMMddHHmmss}-{buyer.Id}",
                TransferContent = $"EZB-{buyer.Id}-{seq}",
                Payload = "{}",
                PaidAt = paidAt,
                CreatedAt = createdAt
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);

            var deposit = new AuctionDeposit
            {
                AuctionId = auction.Id,
                UserId = buyer.Id,
                Amount = depositAmount,
                State = state,
                PaymentId = payment.Id,
                HeldAt = state != DepositState.PendingPayment ? paidAt : null,
                AppliedAt = state == DepositState.Applied ? paidAt.AddDays(1) : null,
                RefundedAt = state == DepositState.Refunded ? (paidAt.AddDays(Rng.Next(1, 5)) is var rd && rd > now ? now.AddMinutes(-10) : rd) : null,
                CreatedAt = createdAt
            };
            db.AuctionDeposits.Add(deposit);

            // For Refunded deposits → also create a Refund record
            if (state == DepositState.Refunded)
            {
                // Persist first so deposit.Id is assigned for the REF-DEP reference.
                await db.SaveChangesAsync(ct);

                var rawRefundedAt = paidAt.AddDays(Rng.Next(1, 5));
                var refundedAt = rawRefundedAt > now ? now.AddMinutes(-Rng.Next(5, 60)) : rawRefundedAt;
                var refund = new Refund
                {
                    PaymentId = payment.Id,
                    Amount = depositAmount,
                    Reason = "Auction deposit refund — losing bidder.",
                    Status = RefundStatus.Completed,
                    ProviderRef = $"REF-DEP-{refundedAt:yyyyMMddHHmmss}-{deposit.Id}",
                    ProcessedAt = refundedAt,
                    CreatedAt = refundedAt.AddMinutes(-5)
                };
                db.Refunds.Add(refund);
                await db.SaveChangesAsync(ct);

                deposit.RefundId = refund.Id;
            }

            await db.SaveChangesAsync(ct);
        }

        // ----------------------------------------------------------------
        // 2. Dispute/Order Refunds (Pending + Completed)
        // ----------------------------------------------------------------
        if (completedOrders.Count > 0)
        {
            // Find payments linked to these orders
            var orderIds = completedOrders.Select(o => o.Id).ToList();
            var paymentOrders = db.PaymentOrders
                .Where(po => orderIds.Contains(po.OrderId))
                .ToList();

            var processedCount = 0;
            foreach (var po in paymentOrders.Take(8))
            {
                var order = completedOrders.First(o => o.Id == po.OrderId);
                var refundAmount = Math.Round(order.Total * (decimal)(Rng.Next(20, 60) / 100.0), 0);
                var daysAgo = Rng.Next(2, 20);
                var createdAt = now.AddDays(-daysAgo);

                // Alternate between Pending and Completed
                var isCompleted = processedCount % 2 == 0;
                var maxProcessedAt = now.AddMinutes(-5); // never in the future
                var rawProcessedAt = createdAt.AddDays(Rng.Next(1, 4));
                var processedAt = isCompleted
                    ? (DateTimeOffset?)(rawProcessedAt > maxProcessedAt ? maxProcessedAt : rawProcessedAt)
                    : null;
                var providerRef = isCompleted
                    ? $"REF-DSP-{createdAt:yyyyMMddHHmmss}-{po.OrderId}"
                    : null;

                var refund = new Refund
                {
                    PaymentId = po.PaymentId,
                    OrderId = po.OrderId,
                    Amount = refundAmount,
                    Reason = isCompleted
                        ? "Partial refund approved by admin after dispute resolution."
                        : "Dispute approved — pending manual bank transfer to buyer.",
                    Status = isCompleted ? RefundStatus.Completed : RefundStatus.Pending,
                    ProviderRef = providerRef,
                    ProcessedAt = processedAt,
                    CreatedAt = createdAt
                };
                db.Refunds.Add(refund);
                processedCount++;
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
