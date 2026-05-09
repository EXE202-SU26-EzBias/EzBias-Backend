namespace EzBias.Application.Features.Orders.Dtos;

public record OrderUserSummary(long Id, string Username, string FullName, string AvatarUrl);
public record OrderSellerSummary(long Id, string Username, string FullName, string AvatarUrl, decimal AvgSellerRating, int TotalRatings);
public record OrderItemSummary(long Id, long? ProductId, string ProductName, string ProductImage, int Quantity, decimal UnitPrice, decimal Subtotal);

public record OrderViewResponse(
    long Id,
    long UserId,
    long SellerId,
    EzBias.Domain.Enums.OrderSource Source,
    long? AuctionId,
    decimal Total,
    EzBias.Domain.Enums.OrderStatus Status,
    string AddressSnap,
    string? Carrier,
    string? TrackingNumber,
    DateTimeOffset CreatedAt,
    OrderUserSummary? User,
    OrderSellerSummary? Seller,
    IReadOnlyList<OrderItemSummary> Items
);
