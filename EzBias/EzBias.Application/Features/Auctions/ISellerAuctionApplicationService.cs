using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions;

public interface ISellerAuctionApplicationService
{
    Task<(bool Success, string? Error, AuctionActionResponse? Data)> CreateAsync(long sellerId, CreateAuctionRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, AuctionActionResponse? Data)> PublishAsync(long sellerId, long auctionId, CancellationToken ct);
    Task<(bool Success, string? Error, AuctionActionResponse? Data)> CancelAsync(long sellerId, long auctionId, CancellationToken ct);
    Task<(bool Success, string? Error, AuctionActionResponse? Data)> RelistAsync(long sellerId, long auctionId, RelistAuctionRequest request, CancellationToken ct);
    Task<IReadOnlyList<SellerAuctionItem>> GetMyAuctionsAsync(long sellerId, AuctionStatus? status, CancellationToken ct);
}
