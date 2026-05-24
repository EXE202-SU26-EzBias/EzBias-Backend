using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Users.Dtos;

public record SellerDashboardResponse(
    // Revenue
    decimal GrossRevenue,
    decimal CommissionPaid,
    decimal NetRevenue,

    // Orders
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    int ShippedOrders,
    int DeliveredOrders,
    int CompletedOrders,
    int CanceledOrders,

    // Payouts
    int PendingPayouts,
    int PaidPayouts,
    decimal PendingPayoutAmount,
    decimal PaidPayoutAmount,

    // Auctions
    int TotalAuctions,
    int LiveAuctions,
    int SoldAuctions,

    // Ratings
    decimal AvgRating,
    int TotalRatings
);
