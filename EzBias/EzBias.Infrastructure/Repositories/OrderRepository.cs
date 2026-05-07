using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly EzBiasDbContext _db;

    public OrderRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void AddRange(IEnumerable<Order> orders) => _db.Orders.AddRange(orders);

    public void Add(Order order) => _db.Orders.Add(order);

    public Task<bool> ExistsByAuctionIdAsync(long auctionId, CancellationToken ct)
        => _db.Orders.AnyAsync(x => x.AuctionId == auctionId, ct);

    public Task<Order?> GetByAuctionIdAsync(long auctionId, CancellationToken ct)
        => _db.Orders.FirstOrDefaultAsync(x => x.AuctionId == auctionId, ct);

    public Task<Order?> GetByIdAsync(long orderId, CancellationToken ct)
        => _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, ct);
}
