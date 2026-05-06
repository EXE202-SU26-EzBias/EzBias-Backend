using EzBias.Application.Features.Auctions.Dtos;

namespace EzBias.Application.Features.Auctions;

public interface IAuctionClosingApplicationService
{
    Task<CloseAuctionsResponse> CloseExpiredAsync(CancellationToken ct);
}
