using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class EscrowRepository : IEscrowRepository
{
    private readonly EzBiasDbContext _db;

    public EscrowRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsHoldByPaymentIdAsync(long paymentId, CancellationToken ct)
        => _db.EscrowTransactions.AnyAsync(x => x.PaymentId == paymentId && x.Type == EscrowType.IN, ct);

    public void AddRange(IEnumerable<EscrowTransaction> transactions)
        => _db.EscrowTransactions.AddRange(transactions);
}
