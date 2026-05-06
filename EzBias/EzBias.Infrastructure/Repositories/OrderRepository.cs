using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;

namespace EzBias.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly EzBiasDbContext _db;

    public OrderRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void AddRange(IEnumerable<Order> orders) => _db.Orders.AddRange(orders);
}
