using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IDisputeRepository
{
    void Add(Dispute dispute);
    void AddItems(IEnumerable<DisputeItem> items);
    Task<Dispute?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<DisputeItem>> GetItemsByDisputeIdAsync(long disputeId, CancellationToken ct);
    Task<Dispute?> GetOpenByOrderIdAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<Dispute>> GetByOrderIdAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<Dispute>> GetAllWithOrderAndBuyerAsync(CancellationToken ct);
}
