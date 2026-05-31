using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class AuctionDepositRepository : IAuctionDepositRepository
{
    private readonly EzBiasDbContext _db;

    public AuctionDepositRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(AuctionDeposit deposit) => _db.Set<AuctionDeposit>().Add(deposit);

    public Task<AuctionDeposit?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Set<AuctionDeposit>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AuctionDeposit?> GetByPaymentIdAsync(long paymentId, CancellationToken ct)
        => _db.Set<AuctionDeposit>().FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct);

    public Task<AuctionDeposit?> GetActiveByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct)
        => _db.Set<AuctionDeposit>().FirstOrDefaultAsync(
            x => x.UserId == userId
                && x.AuctionId == auctionId
                && (x.State == DepositState.PendingPayment || x.State == DepositState.Held),
            ct);

    public Task<AuctionDeposit?> GetLatestByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct)
        => _db.Set<AuctionDeposit>()
            .Where(x => x.UserId == userId && x.AuctionId == auctionId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public Task<bool> HasHeldDepositAsync(long userId, long auctionId, CancellationToken ct)
        => _db.Set<AuctionDeposit>().AnyAsync(
            x => x.UserId == userId && x.AuctionId == auctionId && x.State == DepositState.Held,
            ct);

    public Task<AuctionDeposit?> GetHeldByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct)
        => _db.Set<AuctionDeposit>().FirstOrDefaultAsync(
            x => x.UserId == userId && x.AuctionId == auctionId && x.State == DepositState.Held,
            ct);

    public async Task<IReadOnlyList<AuctionDeposit>> GetHeldByAuctionAsync(long auctionId, long? excludeUserId, CancellationToken ct)
        => await _db.Set<AuctionDeposit>()
            .Where(x => x.AuctionId == auctionId
                && x.State == DepositState.Held
                && (excludeUserId == null || x.UserId != excludeUserId.Value))
            .ToListAsync(ct);
}
