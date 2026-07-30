namespace EzBias.Application.Features.Admin.Dtos;

public record AdminDashboardOverviewResponse(
    int TotalUsers,
    int TotalReviews,
    decimal AvgReviewStars,
    int NewUsersToday,
    int NewUsersLast7Days,
    int NewUsersLast30Days,
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    int ProcessingOrders,
    int ShippedOrders,
    int DeliveredOrders,
    int ReturnRequestedOrders,
    int CompletedOrders,
    int CanceledOrders,
    int RefundedOrders,
    decimal GrossRevenue,
    decimal RefundedAmount,
    decimal NetRevenue,
    decimal TotalCommissionRevenue,
    decimal CommissionRevenueToday,
    decimal CommissionRevenueLast7Days,
    decimal CommissionRevenueLast30Days,
    int OpenDisputes,
    int PendingRefunds,
    int PendingPayouts,
    IReadOnlyList<AdminTopSellerCommissionResponse> TopSellersByNetRevenue,
    IReadOnlyList<AdminMonthlySalesResponse> MonthlySales
);

public record AdminMonthlySalesResponse(
    string Month,
    string Label,
    int OrderCount,
    decimal GrossSales,
    decimal CommissionRevenue,
    decimal SellerNetAmount
);

public record AdminTopSellerCommissionResponse(
    long SellerId,
    string Username,
    string FullName,
    int OrderCount,
    decimal GrossRevenue,
    decimal CommissionRevenue,
    decimal NetRevenue
);

public record AdminUserListItemResponse(
    long Id,
    string FullName,
    string Username,
    string Email,
    string Role,
    bool IsDeleted,
    DateTimeOffset CreatedAt
);

public record AdminUserListResponse(
    IReadOnlyList<AdminUserListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

public record AdminUserDetailResponse(
    long Id,
    string FullName,
    string Username,
    string Email,
    string Role,
    string Phone,
    string City,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int OrdersAsBuyer,
    int OrdersAsSeller,
    int DisputesOpened,
    decimal AvgSellerRating,
    int TotalRatings
);

public record AdminUserListQuery(
    string? Keyword,
    string? Role,
    bool? IsDeleted,
    int Page = 1,
    int PageSize = 20
);

public record AdminCreateUserRequest(
    string FullName,
    string Username,
    string Email,
    string Password,
    string Role,
    string? Phone,
    string? City
);

public record AdminUpdateUserRequest(
    string? FullName,
    string? Username,
    string? Email,
    string? Role,
    string? Phone,
    string? City
);

public record AdminTransactionItem(
    long Id,
    string Kind,           // "payment" | "payout"
    string Direction,      // "in" = buyer→platform | "out" = platform→seller
    decimal Amount,
    string Status,         // "Paid","Pending","Approved","Rejected", etc.
    string Reference,      // SePay reference or bank-transfer ref
    long? OrderId,
    string? BuyerUsername,
    string? BuyerEmail,
    string? SellerUsername,
    string? SellerEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SettledAt  // PaidAt for payment, PaidAt for payout
);
