using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(long id, CancellationToken ct);
    Task<Conversation?> GetByParticipantsAsync(long buyerId, long sellerId, CancellationToken ct);
    Task<IReadOnlyList<Conversation>> GetByUserAsync(long userId, CancellationToken ct);
    void Add(Conversation conversation);
}
