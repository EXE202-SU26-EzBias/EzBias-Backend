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

    public Task<Payment?> GetByIdWithOrdersAsync(long paymentId, CancellationToken ct)
        => _db.Payments
            .Include(x => x.PaymentOrders)
                .ThenInclude(po => po.Order)
            .FirstOrDefaultAsync(x => x.Id == paymentId, ct);
}
