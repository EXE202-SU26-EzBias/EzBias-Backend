using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IPaymentRepository
{
    void Add(Payment payment);
    Task<bool> ExistsByOrderIdAsync(long orderId, CancellationToken ct);
    Task<Payment?> GetPendingByAuctionIdAsync(long auctionId, CancellationToken ct);
    Task<Payment?> GetByReferenceAsync(string reference, CancellationToken ct);
    Task<Payment?> GetByIdAsync(long paymentId, CancellationToken ct);
    Task<Payment?> GetByIdWithOrdersAsync(long paymentId, CancellationToken ct);
    Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken ct);
}
