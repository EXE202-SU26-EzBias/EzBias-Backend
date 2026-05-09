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

    public Task<Auction?> GetByIdWithRelationsAsync(long auctionId, CancellationToken ct)
        => _db.Auctions
            .Include(x => x.Product)
            .Include(x => x.Seller)
            .FirstOrDefaultAsync(x => x.Id == auctionId, ct);

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

        return await query
            .Include(x => x.Product)
            .Include(x => x.Seller)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Auction>> GetPublicAsync(AuctionStatus? status, CancellationToken ct)
    {
        var query = _db.Auctions.AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        else query = query.Where(x => x.Status == AuctionStatus.Live || x.Status == AuctionStatus.Extended);

        return await query
            .Include(x => x.Product)
            .Include(x => x.Seller)
            .OrderBy(x => x.EndsAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Auction>> GetClosableAsync(DateTimeOffset now, CancellationToken ct)
        => await _db.Auctions
            .Include(x => x.Product)
            .Where(x => (x.Status == AuctionStatus.Live || x.Status == AuctionStatus.Extended) && x.EndsAt <= now)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Auction>> GetPendingPaymentExpiredAsync(DateTimeOffset now, CancellationToken ct)
        => await _db.Auctions
            .Where(x => x.Status == AuctionStatus.EndedPendingPayment
                && x.WinnerPaymentDeadline.HasValue
                && x.WinnerPaymentDeadline.Value <= now)
            .ToListAsync(ct);

    public Task<Auction?> GetByOrderIdAsync(long orderId, CancellationToken ct)
        => _db.Auctions.FirstOrDefaultAsync(x => x.Orders.Any(o => o.Id == orderId), ct);

    public async Task<IReadOnlyList<Auction>> GetWonByBuyerAsync(long buyerId, bool onlyPendingPayment, CancellationToken ct)
    {
        var query = _db.Auctions.Where(x => x.WinnerId == buyerId);
        if (onlyPendingPayment) query = query.Where(x => x.Status == AuctionStatus.EndedPendingPayment);
        return await query.OrderByDescending(x => x.EndedAt ?? x.UpdatedAt ?? x.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Auction>> GetEndedBySellerAsync(long sellerId, CancellationToken ct)
        => await _db.Auctions
            .Where(x => x.SellerId == sellerId && (x.Status == AuctionStatus.EndedNoWinner || x.Status == AuctionStatus.EndedPendingPayment || x.Status == AuctionStatus.WinnerFailed || x.Status == AuctionStatus.Sold))
            .OrderByDescending(x => x.EndedAt ?? x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync(ct);

    public void Add(Auction auction) => _db.Auctions.Add(auction);
}
