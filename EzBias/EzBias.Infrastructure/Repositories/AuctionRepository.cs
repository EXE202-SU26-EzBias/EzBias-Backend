using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly EzBiasDbContext _db;

    public AuctionRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<Auction?> GetByIdAsync(long auctionId, CancellationToken ct)
        => _db.Auctions.FirstOrDefaultAsync(x => x.Id == auctionId, ct);

    public Task<Auction?> GetByIdWithProductAsync(long auctionId, CancellationToken ct)
        => _db.Auctions.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == auctionId, ct);

    public Task<bool> ExistsLiveByProductIdAsync(long productId, CancellationToken ct)
        => _db.Auctions.AnyAsync(x => x.ProductId == productId && (x.Status == AuctionStatus.Live || x.Status == AuctionStatus.Extended), ct);

    public Task<bool> ExistsDraftOrLiveByProductIdAsync(long productId, CancellationToken ct)
        => _db.Auctions.AnyAsync(x => x.ProductId == productId && (x.Status == AuctionStatus.Draft || x.Status == AuctionStatus.Live || x.Status == AuctionStatus.Extended), ct);

    public Task<bool> HasAnyBidAsync(long auctionId, CancellationToken ct)
        => _db.Bids.AnyAsync(x => x.AuctionId == auctionId, ct);

    public async Task<IReadOnlyList<Auction>> GetBySellerAsync(long sellerId, AuctionStatus? status, CancellationToken ct)
    {
        var query = _db.Auctions.Where(x => x.SellerId == sellerId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Auction>> GetPublicAsync(AuctionStatus? status, CancellationToken ct)
    {
        var query = _db.Auctions.AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        else query = query.Where(x => x.Status == AuctionStatus.Live || x.Status == AuctionStatus.Extended);

        return await query.OrderBy(x => x.EndsAt).ToListAsync(ct);
    }

    public void Add(Auction auction) => _db.Auctions.Add(auction);
}
