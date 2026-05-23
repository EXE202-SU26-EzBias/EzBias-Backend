using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface ICommissionRepository
{
    Task<bool> ExistsByPaymentIdAsync(long paymentId, CancellationToken ct);
    Task<CommissionTransaction?> GetByOrderIdAsync(long orderId, CancellationToken ct);
    void AddRange(IEnumerable<CommissionTransaction> transactions);
}
