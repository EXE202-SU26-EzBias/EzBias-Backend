using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IEscrowRepository
{
    Task<bool> ExistsHoldByPaymentIdAsync(long paymentId, CancellationToken ct);
    Task<bool> ExistsReleaseByOrderIdAsync(long orderId, CancellationToken ct);
    void AddRange(IEnumerable<EscrowTransaction> transactions);
}
