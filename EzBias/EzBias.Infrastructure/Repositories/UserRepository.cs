using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly EzBiasDbContext _db;

    public UserRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsByEmailOrUsernameAsync(string normalizedEmail, string normalizedUsername, CancellationToken ct)
        => _db.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail || x.Username.ToLower() == normalizedUsername, ct);

    public Task<User?> GetByLoginKeyAsync(string normalizedKey, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedKey || x.Username.ToLower() == normalizedKey, ct);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, ct);

    public Task<User?> GetByIdAsync(long userId, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);

    public void Add(User user) => _db.Users.Add(user);
}
