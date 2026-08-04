using EzBias.Domain.Entities;

namespace EzBias.Domain.Interfaces;

public interface IAuctionDepositRepository
{
    void Add(AuctionDeposit deposit);

    Task<AuctionDeposit?> GetByIdAsync(long id, CancellationToken ct);
    Task<AuctionDeposit?> GetByPaymentIdAsync(long paymentId, CancellationToken ct);

    Task<AuctionDeposit?> GetActiveByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct);

    Task<AuctionDeposit?> GetLatestByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct);

    Task<bool> HasHeldDepositAsync(long userId, long auctionId, CancellationToken ct);

    Task<AuctionDeposit?> GetHeldByUserAndAuctionAsync(long userId, long auctionId, CancellationToken ct);

    Task<IReadOnlyList<AuctionDeposit>> GetHeldByAuctionAsync(long auctionId, long? excludeUserId, CancellationToken ct);

    Task<IReadOnlyList<AuctionDeposit>> GetAllHeldDepositsForAdminAsync(CancellationToken ct);
}
