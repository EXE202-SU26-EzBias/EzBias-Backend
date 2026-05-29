using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly EzBiasDbContext _db;

    public MessageRepository(EzBiasDbContext db) => _db = db;

    public async Task<IReadOnlyList<Message>> GetPageAsync(
        long conversationId, long? before, int pageSize, CancellationToken ct)
    {
        var query = _db.Messages
            .Include(x => x.Sender)
            .Where(x => x.ConversationId == conversationId);

        if (before.HasValue)
            query = query.Where(x => x.Id < before.Value);

        return await query
            .OrderByDescending(x => x.SentAt)
            .Take(pageSize)
            .OrderBy(x => x.SentAt) // return in ascending order
            .ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(long conversationId, long recipientId, CancellationToken ct)
        => _db.Messages.CountAsync(
            x => x.ConversationId == conversationId
              && x.SenderId != recipientId
              && !x.IsRead,
            ct);

    public async Task<IReadOnlyList<Message>> GetUnreadByRecipientAsync(
        long conversationId, long recipientId, CancellationToken ct)
        => await _db.Messages
            .Where(x => x.ConversationId == conversationId
                     && x.SenderId != recipientId
                     && !x.IsRead)
            .ToListAsync(ct);

    public void Add(Message message) => _db.Messages.Add(message);
}
