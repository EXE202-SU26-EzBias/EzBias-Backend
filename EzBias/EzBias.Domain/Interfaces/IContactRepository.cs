using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IContactRepository
{
    void Add(ContactMessage message);
}
