using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class BidRepository : IBidRepository
{
    private readonly EzBiasDbContext _db;

    public BidRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(Bid bid) => _db.Bids.Add(bid);

    public async Task<decimal?> GetHighestBidAmountAsync(long auctionId, CancellationToken ct)
        => await _db.Bids
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.Amount)
            .Select(x => (decimal?)x.Amount)
            .FirstOrDefaultAsync(ct);

    public Task ClearWinningFlagsAsync(long auctionId, CancellationToken ct)
        => _db.Bids
            .Where(x => x.AuctionId == auctionId && x.IsWinning)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.IsWinning, false),
                ct);

    public Task<Bid?> GetTopBidAsync(long auctionId, CancellationToken ct)
        => _db.Bids
            .AsNoTracking()
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.PlacedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Bid>> GetByAuctionIdAsync(long auctionId, CancellationToken ct)
        => await _db.Bids
            .Include(x => x.User)
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.PlacedAt)
            .ToListAsync(ct);
}
