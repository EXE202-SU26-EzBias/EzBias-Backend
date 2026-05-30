using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface ICallSessionRepository
{
    Task<CallSession?> GetByIdAsync(long id, CancellationToken ct);
    Task<CallSession?> GetActiveByConversationAsync(long conversationId, CancellationToken ct);
    Task<IReadOnlyList<CallSession>> GetByConversationAsync(long conversationId, CancellationToken ct);
    void Add(CallSession callSession);
}
