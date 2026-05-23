using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class CommissionRepository : ICommissionRepository
{
    private readonly EzBiasDbContext _db;

    public CommissionRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsByPaymentIdAsync(long paymentId, CancellationToken ct)
        => _db.CommissionTransactions.AnyAsync(x => x.PaymentId == paymentId, ct);

    public Task<CommissionTransaction?> GetByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.CommissionTransactions.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

    public void AddRange(IEnumerable<CommissionTransaction> transactions)
        => _db.CommissionTransactions.AddRange(transactions);
}
