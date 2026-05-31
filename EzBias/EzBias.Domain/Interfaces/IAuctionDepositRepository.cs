using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IAuctionDepositRepository
{
    void Add(AuctionDeposit deposit);

    Task<AuctionDeposit?> GetByIdAsync(long id, CancellationToken ct);
    Task<AuctionDeposit?> GetByPaymentIdAsync(long paymentId, CancellationToken ct);

    // The single active (PendingPayment OR Held) deposit for a user+auction, if any (Req 10.3/10.5).
    Task<AuctionDeposit?> GetActiveByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct);

    // Most recent deposit for user+auction regardless of state (Req 9 status lookup).
    Task<AuctionDeposit?> GetLatestByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct);

    // Gate check (Req 4): true iff a Held deposit exists for user+auction.
    Task<bool> HasHeldDepositAsync(long userId, long auctionId, CancellationToken ct);

    // Held deposit belonging to a specific user (winner) for an auction (Req 6, 7).
    Task<AuctionDeposit?> GetHeldByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct);

    // All Held deposits for an auction excluding an optional userId (Req 5 non-winner refunds).
    Task<IReadOnlyList<AuctionDeposit>> GetHeldByAuctionAsync(long auctionId, long? excludeUserId, CancellationToken ct);
}
