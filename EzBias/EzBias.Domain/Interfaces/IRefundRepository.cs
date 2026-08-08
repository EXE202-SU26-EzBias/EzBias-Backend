using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IRefundRepository
{
    void Add(Refund refund);
    Task<Refund?> GetByIdAsync(long id, CancellationToken ct);
    Task<decimal> GetProcessedTotalByPaymentIdAsync(long paymentId, CancellationToken ct);
    Task<Refund?> GetLatestByDisputeIdAsync(long disputeId, CancellationToken ct);
    Task<IReadOnlyList<Refund>> GetByOrderIdAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(long paymentId, CancellationToken ct);
}
