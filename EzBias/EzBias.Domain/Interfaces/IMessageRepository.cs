using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IMessageRepository
{
    Task<IReadOnlyList<Message>> GetPageAsync(
        long conversationId, long? before, int pageSize, CancellationToken ct);
    Task<int> CountUnreadAsync(long conversationId, long recipientId, CancellationToken ct);
    Task<IReadOnlyList<Message>> GetUnreadByRecipientAsync(
        long conversationId, long recipientId, CancellationToken ct);
    void Add(Message message);
}
