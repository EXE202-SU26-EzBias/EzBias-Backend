using System.Text.Json;
using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Orders;

public class OrderApplicationService : IOrderApplicationService
{
    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IEscrowRepository _escrows;
    private readonly IPayoutRepository _payouts;
    private readonly IUnitOfWork _uow;

    public OrderApplicationService(IOrderRepository orders, ICartRepository carts, IEscrowRepository escrows, IPayoutRepository payouts, IUnitOfWork uow)
    {
        _orders = orders;
        _carts = carts;
        _escrows = escrows;
        _payouts = payouts;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, CreateOrderResponse? Data)> CreateAsync(long userId, CreateOrderRequest request, CancellationToken ct)
    {
        if (request.CartItemIds is null || request.CartItemIds.Count == 0)
            return (false, "Please select at least one cart item.", null);

        var cartItems = await _carts.GetByUserIdAndIdsAsync(userId, request.CartItemIds.Distinct().ToList(), ct);
        if (cartItems.Count == 0)
            return (false, "No cart items found.", null);

        foreach (var item in cartItems)
        {
            if (item.Product.DeletedAt is not null || item.Product.Status != ProductStatus.Active || item.Product.IsAuction)
                return (false, $"Product '{item.Product.Name}' is not available for checkout.", null);

            if (item.Product.Stock < item.Quantity)
                return (false, $"Product '{item.Product.Name}' does not have enough stock.", null);
        }

        var normalizedAddressSnap = NormalizeAddressSnap(request.AddressSnap);
        if (normalizedAddressSnap is null)
            return (false, "addressSnap must be valid JSON or plain text.", null);

        var sellerGroups = cartItems
            .GroupBy(x => x.Product.SellerId)
            .Select(g => new
            {
                SellerId = g.Key,
                Items = g.ToList(),
                Total = g.Sum(i => i.Product.Price * i.Quantity)
            })
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var orderList = sellerGroups.Select(g => new Order
        {
            UserId = userId,
            SellerId = g.SellerId,
            Source = OrderSource.Cart,
            Total = g.Total,
            Status = OrderStatus.Pending,
            AddressSnap = normalizedAddressSnap,
            CreatedAt = now,
            Items = g.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ProductImage = i.Product.PrimaryImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.Product.Price,
                Subtotal = i.Product.Price * i.Quantity
            }).ToList()
        }).ToList();

        _orders.AddRange(orderList);
        _carts.RemoveRange(cartItems);
        await _uow.SaveChangesAsync(ct);

        return (true, null, new CreateOrderResponse(orderList.Select(x => x.Id).ToList()));
    }

    public async Task<IReadOnlyList<OrderViewResponse>> GetByBuyerAsync(long userId, CancellationToken ct)
    {
        var items = await _orders.GetByBuyerAsync(userId, ct);
        return items.Select(Map).ToList();
    }

    public async Task<(bool Success, string? Error, OrderViewResponse? Data)> GetDetailAsync(long userId, long orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdWithItemsAsync(orderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.UserId != userId && order.SellerId != userId) return (false, "Forbidden.", null);
        return (true, null, Map(order));
    }

    public async Task<IReadOnlyList<OrderViewResponse>> GetBySellerAsync(long sellerId, CancellationToken ct)
    {
        var items = await _orders.GetBySellerAsync(sellerId, ct);
        return items.Select(Map).ToList();
    }

    public async Task<(bool Success, string? Error, FulfillmentActionResponse? Data)> ConfirmReceivedAsync(long userId, long orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.UserId != userId) return (false, "Forbidden.", null);
        if (order.Status != OrderStatus.Shipped && order.Status != OrderStatus.Delivered)
            return (false, "Order cannot be confirmed in current status.", null);

        order.DeliveredAt = DateTimeOffset.UtcNow;
        order.CompletedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _escrows.AddRange(new[]
        {
            new EscrowTransaction
            {
                OrderId = order.Id,
                SellerId = order.SellerId,
                Type = EscrowType.OUT,
                Amount = order.Total,
                CreatedAt = DateTimeOffset.UtcNow
            }
        });

        var payout = await _payouts.GetByOrderIdAsync(order.Id, ct);
        if (payout is null)
        {
            _payouts.Add(new Payout
            {
                OrderId = order.Id,
                SellerId = order.SellerId,
                Amount = order.Total,
                Status = PayoutStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _uow.SaveChangesAsync(ct);
        return (true, null, new FulfillmentActionResponse(order.Id, order.Status.ToString()));
    }

    private static OrderViewResponse Map(Order o)
        => new(
            o.Id,
            o.UserId,
            o.SellerId,
            o.Source,
            o.AuctionId,
            o.Total,
            o.Status,
            o.AddressSnap,
            o.Carrier,
            o.TrackingNumber,
            o.CreatedAt,
            o.User is null ? null : new OrderUserSummary(o.User.Id, o.User.Username, o.User.FullName, o.User.AvatarUrl),
            o.Seller is null ? null : new OrderSellerSummary(o.Seller.Id, o.Seller.Username, o.Seller.FullName, o.Seller.AvatarUrl, o.Seller.AvgSellerRating, o.Seller.TotalRatings),
            o.Items.Select(i => new OrderItemSummary(i.Id, i.ProductId, i.ProductName, i.ProductImage, i.Quantity, i.UnitPrice, i.Subtotal)).ToList());

    private static string? NormalizeAddressSnap(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "{}";

        var trimmed = input.Trim();

        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            try
            {
                using var _ = JsonDocument.Parse(trimmed);
                return trimmed;
            }
            catch
            {
                return null;
            }
        }

        return JsonSerializer.Serialize(new { fullAddress = trimmed });
    }
}
