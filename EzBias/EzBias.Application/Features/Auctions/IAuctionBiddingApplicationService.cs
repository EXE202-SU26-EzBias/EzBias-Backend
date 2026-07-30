using EzBias.Application.Common.Results;
using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions;

public interface IAuctionBiddingApplicationService
{
    Task<IReadOnlyList<AuctionListItem>> GetPublicAuctionsAsync(AuctionStatus? status, CancellationToken ct);
    Task<Result<AuctionDetailItem>> GetDetailAsync(long auctionId, CancellationToken ct);
    Task<IReadOnlyList<BidHistoryItem>> GetBidHistoryAsync(long auctionId, CancellationToken ct);
    Task<Result<PlaceBidResponse>> PlaceBidAsync(long bidderId, long auctionId, PlaceBidRequest request, CancellationToken ct);
}
