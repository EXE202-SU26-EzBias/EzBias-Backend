namespace EzBias.Domain.Enums;

public enum NotificationType
{
    Outbid = 1,
    AuctionWon = 2,
    AuctionExpired = 3,
    AuctionEndingSoon = 4,
    OrderPlaced = 5,
    OrderShipped = 6,
    OrderDelivered = 7,
    PayoutPaid = 8,
    DisputeOpened = 9,
    DisputeResolved = 10,
    UserVerified = 11,
    OrderConfirmed = 12,
    NewMessage = 13,
    DepositConfirmed = 14,
    DepositRefundInitiated = 15,
    DepositForfeited = 16,
    DisputeRefundCompleted = 17
}
