using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly EzBiasDbContext _db;

    public NotificationRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(long userId, CancellationToken ct)
        => await _db.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<Notification?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);
}
