using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly EzBiasDbContext _db;

    public ConversationRepository(EzBiasDbContext db) => _db = db;

    public Task<Conversation?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Conversations
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Conversation?> GetByParticipantsAsync(long buyerId, long sellerId, CancellationToken ct)
        => _db.Conversations
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .FirstOrDefaultAsync(x => x.BuyerId == buyerId && x.SellerId == sellerId, ct);

    public async Task<IReadOnlyList<Conversation>> GetByUserAsync(long userId, CancellationToken ct)
        => await _db.Conversations
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Where(x => x.BuyerId == userId || x.SellerId == userId)
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync(ct);

    public void Add(Conversation conversation) => _db.Conversations.Add(conversation);
}
