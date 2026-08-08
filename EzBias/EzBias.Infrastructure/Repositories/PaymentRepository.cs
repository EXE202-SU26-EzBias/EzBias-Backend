using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly EzBiasDbContext _db;

    public PaymentRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(Payment payment) => _db.Payments.Add(payment);

    public Task<bool> ExistsByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.PaymentOrders.AnyAsync(x => x.OrderId == orderId, ct);

    public Task<Payment?> GetPendingByAuctionIdAsync(long auctionId, CancellationToken ct)
        => _db.Payments
            .Include(x => x.PaymentOrders)
                .ThenInclude(po => po.Order)
            .FirstOrDefaultAsync(x => x.Status == Domain.Enums.PaymentStatus.Pending
                && x.PaymentOrders.Any(po => po.Order.AuctionId == auctionId), ct);

    public Task<Payment?> GetByReferenceAsync(string reference, CancellationToken ct)
        => _db.Payments.FirstOrDefaultAsync(x => x.Reference == reference, ct);

    public Task<Payment?> GetByIdAsync(long paymentId, CancellationToken ct)
        => _db.Payments
            .Include(x => x.PaymentOrders)
                .ThenInclude(po => po.Order)
            .FirstOrDefaultAsync(x => x.Id == paymentId, ct);

    public Task<Payment?> GetByIdWithOrdersForUpdateAsync(long paymentId, CancellationToken ct)
        => _db.Payments
            .FromSqlInterpolated($"""
                SELECT *
                FROM payments
                WHERE id = {paymentId}
                FOR UPDATE
                """)
            .Include(x => x.PaymentOrders)
                .ThenInclude(po => po.Order)
                    .ThenInclude(o => o.Items)
                        .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(ct);

    public Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.Payments
            .Include(x => x.PaymentOrders)
                .ThenInclude(po => po.Order)
            .FirstOrDefaultAsync(x => x.PaymentOrders.Any(po => po.OrderId == orderId), ct);
}
