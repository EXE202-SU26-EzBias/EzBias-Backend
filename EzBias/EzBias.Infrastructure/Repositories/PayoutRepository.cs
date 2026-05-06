using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class PayoutRepository : IPayoutRepository
{
    private readonly EzBiasDbContext _db;

    public PayoutRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<Payout?> GetByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.Payouts.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

    public Task<Payout?> GetByIdAsync(long payoutId, CancellationToken ct)
        => _db.Payouts.FirstOrDefaultAsync(x => x.Id == payoutId, ct);

    public async Task<IReadOnlyList<Payout>> GetBySellerAsync(long sellerId, PayoutStatus? status, CancellationToken ct)
    {
        var query = _db.Payouts.Where(x => x.SellerId == sellerId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public void Add(Payout payout) => _db.Payouts.Add(payout);
}
