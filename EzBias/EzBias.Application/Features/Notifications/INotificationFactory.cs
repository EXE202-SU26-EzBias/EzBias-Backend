using EzBias.Domain.Entities;

namespace EzBias.Application.Features.Notifications;

/// <summary>
/// Creates Notification entities for common domain events.
/// Caller is responsible for persisting via INotificationRepository.
/// </summary>
public interface INotificationFactory
{
    Notification Outbid(long userId, long auctionId, string productName, decimal newBid);
    Notification AuctionWon(long userId, long auctionId, string productName, decimal finalPrice);
    Notification AuctionExpired(long userId, long auctionId, string productName);
    Notification AuctionEndingSoon(long userId, long auctionId, string productName, int minutesLeft);
    Notification OrderPlaced(long sellerId, long orderId, string productNames);
    Notification OrderShipped(long userId, long orderId, string? trackingNumber);
    Notification OrderDelivered(long userId, long orderId);
    Notification PayoutPaid(long sellerId, long payoutId, decimal amount);
    Notification DisputeOpened(long sellerId, long disputeId, long orderId);
    Notification DisputeResolved(long userId, long disputeId, bool resolvedForBuyer);
    Notification DisputeRefundCompleted(long userId, long disputeId, decimal amount);
    Notification UserVerified(long userId);
    Notification OrderConfirmed(long sellerId, long orderId);
    Notification NewMessage(long recipientId, long conversationId, string senderName, string preview);
    Notification DepositConfirmed(long userId, long auctionId, string productName, decimal amount);
    Notification DepositRefundInitiated(long userId, long auctionId, string productName, decimal amount);
    Notification DepositForfeited(long userId, long auctionId, string productName, decimal amount);
}
