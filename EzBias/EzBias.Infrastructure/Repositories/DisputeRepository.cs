using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly EzBiasDbContext _db;

    public DisputeRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(Dispute dispute) => _db.Disputes.Add(dispute);

    public void AddItems(IEnumerable<DisputeItem> items) => _db.DisputeItems.AddRange(items);

    public Task<Dispute?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Disputes
            .Include(x => x.Order)
            .Include(x => x.Items)
                .ThenInclude(i => i.OrderItem)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<DisputeItem>> GetItemsByDisputeIdAsync(long disputeId, CancellationToken ct)
        => await _db.DisputeItems
            .Include(x => x.OrderItem)
            .Where(x => x.DisputeId == disputeId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

    public Task<Dispute?> GetOpenByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.Disputes.FirstOrDefaultAsync(x => x.OrderId == orderId && (x.Status == DisputeStatus.Open || x.Status == DisputeStatus.UnderReview), ct);

    public async Task<IReadOnlyList<Dispute>> GetByOrderIdAsync(long orderId, CancellationToken ct)
        => await _db.Disputes.Where(x => x.OrderId == orderId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Dispute>> GetAllWithOrderAndBuyerAsync(CancellationToken ct)
        => await _db.Disputes
            .Include(x => x.Order)
                .ThenInclude(o => o.User)
            .Include(x => x.Items)
                .ThenInclude(i => i.OrderItem)
            .Include(x => x.Refunds)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}
