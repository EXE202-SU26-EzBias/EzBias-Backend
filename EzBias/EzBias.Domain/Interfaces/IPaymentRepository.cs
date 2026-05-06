using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IPaymentRepository
{
    void Add(Payment payment);
}
