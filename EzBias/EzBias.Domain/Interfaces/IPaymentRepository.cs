using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IPaymentRepository
{
    void Add(Payment payment);
    Task<bool> ExistsByOrderIdAsync(long orderId, CancellationToken ct);
    Task<Payment?> GetByIdWithOrdersAsync(long paymentId, CancellationToken ct);
}
