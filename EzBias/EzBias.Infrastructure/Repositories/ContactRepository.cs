using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;

namespace EzBias.Infrastructure.Repositories;

public sealed class ContactRepository : IContactRepository
{
    private readonly EzBiasDbContext _db;

    public ContactRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public void Add(ContactMessage message) => _db.ContactMessages.Add(message);
}
