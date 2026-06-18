using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Users.Dtos;

public record SellerDashboardResponse(
    // Revenue
    decimal GrossRevenue,
    decimal CommissionPaid,
    decimal NetRevenue,

    // Items sold
    int ItemsSold,

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
    int TotalRatings,

    // Monthly trend (last 12 calendar months, oldest first)
    IReadOnlyList<SellerMonthlySalesPoint> MonthlySales,

    // Best-selling listings, ranked by units sold (desc), top 5
    IReadOnlyList<SellerTopListing> TopListings
);

public record SellerTopListing(
    long? ProductId,
    string ProductName,
    string ProductImage,
    int UnitsSold,
    decimal Revenue
);

public record SellerMonthlySalesPoint(
    string Month,          // "yyyy-MM"
    string Label,          // "Jan 2026"
    int ItemsSold,
    int OrderCount,
    decimal GrossRevenue,
    decimal CommissionPaid,
    decimal NetRevenue
);
