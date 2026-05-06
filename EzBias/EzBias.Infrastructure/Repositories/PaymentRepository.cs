using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;

namespace EzBias.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly EzBiasDbContext _db;

    public PaymentRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(Payment payment) => _db.Payments.Add(payment);
}
