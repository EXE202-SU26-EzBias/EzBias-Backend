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

    public async Task ClearWinningFlagsAsync(long auctionId, CancellationToken ct)
    {
        var winning = await _db.Bids.Where(x => x.AuctionId == auctionId && x.IsWinning).ToListAsync(ct);
        foreach (var item in winning)
            item.IsWinning = false;
    }

    public Task<Bid?> GetTopBidAsync(long auctionId, CancellationToken ct)
        => _db.Bids
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.PlacedAt)
            .FirstOrDefaultAsync(ct);
}
