using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Domain.Interfaces;

public interface IPayoutRepository
{
    Task<Payout?> GetByOrderIdAsync(long orderId, CancellationToken ct);
    Task<Payout?> GetByIdAsync(long payoutId, CancellationToken ct);
    Task<IReadOnlyList<Payout>> GetBySellerAsync(long sellerId, PayoutStatus? status, CancellationToken ct);
    Task<IReadOnlyList<Payout>> GetAllAsync(PayoutStatus? status, CancellationToken ct);
    void Add(Payout payout);
}
