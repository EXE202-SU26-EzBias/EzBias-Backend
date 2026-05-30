using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class CallSessionRepository : ICallSessionRepository
{
    private static readonly CallSessionStatus[] ActiveStatuses =
    [
        CallSessionStatus.Ringing,
        CallSessionStatus.Accepted
    ];

    private readonly EzBiasDbContext _db;

    public CallSessionRepository(EzBiasDbContext db) => _db = db;

    public Task<CallSession?> GetByIdAsync(long id, CancellationToken ct)
        => _db.CallSessions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CallSession?> GetActiveByConversationAsync(long conversationId, CancellationToken ct)
        => _db.CallSessions
            .Where(x => x.ConversationId == conversationId && ActiveStatuses.Contains(x.Status))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CallSession>> GetByConversationAsync(long conversationId, CancellationToken ct)
        => await _db.CallSessions
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public void Add(CallSession callSession) => _db.CallSessions.Add(callSession);
}
