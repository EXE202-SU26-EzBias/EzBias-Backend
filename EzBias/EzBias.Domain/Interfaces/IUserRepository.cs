using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Domain.Interfaces;

public interface IUserRepository
{
    Task<bool> ExistsByEmailOrUsernameAsync(string normalizedEmail, string normalizedUsername, CancellationToken ct);
    Task<User?> GetByLoginKeyAsync(string normalizedKey, CancellationToken ct);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<User?> GetByIdAsync(long userId, CancellationToken ct);
    Task<IReadOnlyList<long>> GetUserIdsByRoleAsync(UserRole role, CancellationToken ct);
    void Add(User user);
    void Remove(User user);
}
