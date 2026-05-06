using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IEscrowRepository
{
    Task<bool> ExistsHoldByPaymentIdAsync(long paymentId, CancellationToken ct);
    void AddRange(IEnumerable<EscrowTransaction> transactions);
}
