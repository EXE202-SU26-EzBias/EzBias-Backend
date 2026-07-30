using EzBias.Application.Common.Results;
using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions;

public interface ISellerAuctionApplicationService
{
    Task<Result<AuctionActionResponse>> CreateAsync(long sellerId, CreateAuctionRequest request, CancellationToken ct);
    Task<Result<AuctionActionResponse>> PublishAsync(long sellerId, long auctionId, CancellationToken ct);
    Task<Result<AuctionActionResponse>> CancelAsync(long sellerId, long auctionId, CancellationToken ct);
    Task<Result<AuctionActionResponse>> RelistAsync(long sellerId, long auctionId, RelistAuctionRequest request, CancellationToken ct);
    Task<IReadOnlyList<SellerAuctionItem>> GetMyAuctionsAsync(long sellerId, AuctionStatus? status, CancellationToken ct);
}
