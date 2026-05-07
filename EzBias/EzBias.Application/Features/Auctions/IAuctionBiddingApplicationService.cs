using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions;

public interface IAuctionBiddingApplicationService
{
    Task<IReadOnlyList<AuctionListItem>> GetPublicAuctionsAsync(AuctionStatus? status, CancellationToken ct);
    Task<(bool Success, string? Error, AuctionDetailItem? Data)> GetDetailAsync(long auctionId, CancellationToken ct);
    Task<(bool Success, string? Error, PlaceBidResponse? Data)> PlaceBidAsync(long bidderId, long auctionId, PlaceBidRequest request, CancellationToken ct);
}
