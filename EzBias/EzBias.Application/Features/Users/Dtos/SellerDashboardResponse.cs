using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Users.Dtos;

public record SellerDashboardResponse(
    decimal GrossRevenue,
    decimal CommissionPaid,
    decimal NetRevenue,
    int ItemsSold,
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    int ShippedOrders,
    int DeliveredOrders,
    int CompletedOrders,
    int CanceledOrders,
    int PendingPayouts,
    int PaidPayouts,
    decimal PendingPayoutAmount,
    decimal PaidPayoutAmount,
    int TotalAuctions,
    int LiveAuctions,
    int SoldAuctions,
    decimal AvgRating,
    int TotalRatings,
    IReadOnlyList<SellerMonthlySalesPoint> MonthlySales,
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
    string Month,
    string Label,
    int ItemsSold,
    int OrderCount,
    decimal GrossRevenue,
    decimal CommissionPaid,
    decimal NetRevenue
);
