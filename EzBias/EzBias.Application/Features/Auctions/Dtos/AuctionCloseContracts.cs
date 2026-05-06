namespace EzBias.Application.Features.Auctions.Dtos;

public record CloseAuctionsResponse(int ClosedCount, int EndedNoWinnerCount, int EndedPendingPaymentCount);
