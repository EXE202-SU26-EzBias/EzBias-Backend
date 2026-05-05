namespace EzBias.Domain.Enums;

public enum AuctionStatus
{
    Live = 1,
    Extended = 2,
    EndedNoWinner = 3,
    EndedPendingPayment = 4,
    WinnerFailed = 5,
    Sold = 6,
    Canceled = 7
}
