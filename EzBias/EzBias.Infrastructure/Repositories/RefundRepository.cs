using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly EzBiasDbContext _db;

    public RefundRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(Refund refund) => _db.Refunds.Add(refund);

    public Task<Refund?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Refunds.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<decimal> GetProcessedTotalByPaymentIdAsync(long paymentId, CancellationToken ct)
        => await _db.Refunds.Where(x => x.PaymentId == paymentId && x.Status == RefundStatus.Completed).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

    public Task<bool> ExistsPendingByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.Refunds.AnyAsync(x => x.OrderId == orderId && x.Status == RefundStatus.Pending, ct);

    public Task<Refund?> GetLatestByDisputeIdAsync(long disputeId, CancellationToken ct)
        => _db.Refunds.Where(x => x.DisputeId == disputeId).OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Refund>> GetByOrderIdAsync(long orderId, CancellationToken ct)
        => await _db.Refunds.Where(x => x.OrderId == orderId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(long paymentId, CancellationToken ct)
        => await _db.Refunds.Where(x => x.PaymentId == paymentId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
}
