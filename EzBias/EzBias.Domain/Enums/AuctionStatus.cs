namespace EzBias.Domain.Enums;

public enum AuctionStatus
{
    Draft = 1,
    Live = 2,
    Extended = 3,
    EndedNoWinner = 4,
    EndedPendingPayment = 5,
    WinnerFailed = 6,
    Sold = 7,
    Canceled = 8
}
