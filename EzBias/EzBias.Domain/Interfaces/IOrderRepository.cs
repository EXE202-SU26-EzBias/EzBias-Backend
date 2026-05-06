using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IOrderRepository
{
    void AddRange(IEnumerable<Order> orders);
}
