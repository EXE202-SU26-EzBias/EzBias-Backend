using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IPaymentRepository
{
    void Add(Payment payment);
    Task<Payment?> GetByIdWithOrdersAsync(long paymentId, CancellationToken ct);
}
