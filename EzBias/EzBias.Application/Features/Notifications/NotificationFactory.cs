using System.Text.Json;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Notifications;

public sealed class NotificationFactory : INotificationFactory
{
    public Notification Outbid(long userId, long auctionId, string productName, decimal newBid)
        => Build(userId, NotificationType.Outbid,
            "You've been outbid",
            $"Someone placed a higher bid of {newBid:N0} VND on \"{productName}\".",
            new { auctionId });

    public Notification AuctionWon(long userId, long auctionId, string productName, decimal finalPrice)
        => Build(userId, NotificationType.AuctionWon,
            "You won the auction",
            $"Congratulations! You won \"{productName}\" for {finalPrice:N0} VND. Please complete your payment.",
            new { auctionId });

    public Notification AuctionExpired(long userId, long auctionId, string productName)
        => Build(userId, NotificationType.AuctionExpired,
            "Auction ended with no winner",
            $"The auction for \"{productName}\" ended without a winner.",
            new { auctionId });

    public Notification AuctionEndingSoon(long userId, long auctionId, string productName, int minutesLeft)
        => Build(userId, NotificationType.AuctionEndingSoon,
            $"Auction ending in {minutesLeft} minute{(minutesLeft == 1 ? "" : "s")}",
            $"The auction for \"{productName}\" is ending in {minutesLeft} minute{(minutesLeft == 1 ? "" : "s")}. Place your bid now!",
            new { auctionId, minutesLeft });

    public Notification OrderPlaced(long sellerId, long orderId, string productNames)
        => Build(sellerId, NotificationType.OrderPlaced,
            "New order received",
            $"You have a new order for: {productNames}.",
            new { orderId });

    public Notification OrderShipped(long userId, long orderId, string? trackingNumber)
        => Build(userId, NotificationType.OrderShipped,
            "Your order has been shipped",
            string.IsNullOrWhiteSpace(trackingNumber)
                ? "Your order is on its way."
                : $"Your order is on its way. Tracking: {trackingNumber}",
            new { orderId });

    public Notification OrderDelivered(long userId, long orderId)
        => Build(userId, NotificationType.OrderDelivered,
            "Order delivered",
            "Your order has been marked as delivered. Please confirm receipt.",
            new { orderId });

    public Notification PayoutPaid(long sellerId, long payoutId, decimal amount)
        => Build(sellerId, NotificationType.PayoutPaid,
            "Payout completed",
            $"Your payout of {amount:N0} VND has been transferred to your bank account.",
            new { payoutId });

    public Notification DisputeOpened(long sellerId, long disputeId, long orderId)
        => Build(sellerId, NotificationType.DisputeOpened,
            "Dispute opened on your order",
            "A buyer has opened a dispute on one of your orders. Please check the admin panel.",
            new { disputeId, orderId });

    public Notification DisputeResolved(long userId, long disputeId, bool resolvedForBuyer)
        => Build(userId, NotificationType.DisputeResolved,
            "Dispute resolved",
            resolvedForBuyer
                ? "Your dispute has been resolved in your favor. A refund will be processed."
                : "The dispute on your order has been resolved in the seller's favor.",
            new { disputeId });

    public Notification UserVerified(long userId)
        => Build(userId, NotificationType.UserVerified,
            "Email verified",
            "Your email has been successfully verified. Welcome to EzBias!",
            new { });

    private static Notification Build(long userId, NotificationType type, string title, string body, object meta)
        => new()
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Meta = JsonSerializer.Serialize(meta),
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
