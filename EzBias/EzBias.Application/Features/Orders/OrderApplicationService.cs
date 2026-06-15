using System.Text.Json;
using EzBias.Application.Features.Notifications;
using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Orders;

public class OrderApplicationService : IOrderApplicationService
{
    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IAuctionRepository _auctions;
    private readonly IEscrowRepository _escrows;
    private readonly IPayoutRepository _payouts;
    private readonly ICommissionRepository _commissions;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;

    public OrderApplicationService(
        IOrderRepository orders,
        ICartRepository carts,
        IAuctionRepository auctions,
        IEscrowRepository escrows,
        IPayoutRepository payouts,
        ICommissionRepository commissions,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow)
    {
        _orders = orders;
        _carts = carts;
        _auctions = auctions;
        _escrows = escrows;
        _payouts = payouts;
        _commissions = commissions;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, CreateOrderResponse? Data)> CreateAsync(long userId, CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count == 0)
            return (false, "Please select at least one cart item.", null);

        if (request.Items.Any(x => x.Quantity <= 0))
            return (false, "Quantity must be greater than 0.", null);

        var cartItemIds = request.Items.Select(x => x.CartItemId).Distinct().ToList();
        var cartItems = await _carts.GetByUserIdAndIdsAsync(userId, cartItemIds, ct);
        if (cartItems.Count == 0)
            return (false, "No cart items found.", null);

        var quantityMap = request.Items
            .GroupBy(x => x.CartItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        foreach (var item in cartItems)
        {
            if (!quantityMap.TryGetValue(item.Id, out var newQuantity))
                continue;

            item.Quantity = newQuantity;

            // Allow auction products in cart (added when winner proceeds to payment)
            // but reject if product is deleted or inactive
            if (item.Product.DeletedAt is not null || item.Product.Status != ProductStatus.Active)
                return (false, $"Product '{item.Product.Name}' is not available for checkout.", null);

            if (item.Product.Stock < item.Quantity)
                return (false, $"Product '{item.Product.Name}' does not have enough stock.", null);
        }

        if (request.AddressSnap is null)
            return (false, "address_snap is required.", null);

        var normalizedAddressSnap = NormalizeAddressSnap(request.AddressSnap);
        if (normalizedAddressSnap is null)
            return (false, "address_snap is invalid.", null);

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
        var orderList = new List<Order>();
        
        foreach (var group in sellerGroups)
        {
            var orderItems = new List<OrderItem>();
            decimal orderTotal = 0;
            Order? existingAuctionOrder = null;
            long? auctionId = null;
            
            foreach (var cartItem in group.Items)
            {
                decimal unitPrice = cartItem.Product.Price;
                
                // Check if this product is from a won auction
                var auction = await _auctions.GetByProductIdAndWinnerAsync(cartItem.ProductId, userId, ct);
                if (auction is not null && auction.Status == AuctionStatus.EndedPendingPayment && auction.FinalPrice.HasValue)
                {
                    // Use the final bid price instead of product price
                    unitPrice = auction.FinalPrice.Value;
                    auctionId = auction.Id;
                    
                    // Check if Order already exists from AuctionCloseScheduler
                    existingAuctionOrder = await _orders.GetByAuctionIdAsync(auction.Id, ct);
                }
                
                var subtotal = unitPrice * cartItem.Quantity;
                orderTotal += subtotal;
                
                orderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.Name,
                    ProductImage = cartItem.Product.PrimaryImageUrl,
                    Quantity = cartItem.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = subtotal
                });
            }
            
            // If this is an auction order and Order already exists, UPDATE it instead of creating new
            if (existingAuctionOrder is not null)
            {
                existingAuctionOrder.AddressSnap = normalizedAddressSnap;
                existingAuctionOrder.UpdatedAt = now;
                // Items already exist, just update address
                orderList.Add(existingAuctionOrder);
            }
            else
            {
                // Create new order for regular cart or first-time auction
                orderList.Add(new Order
                {
                    UserId = userId,
                    SellerId = group.SellerId,
                    Source = auctionId.HasValue ? OrderSource.Auction : OrderSource.Cart,
                    AuctionId = auctionId,
                    Total = orderTotal,
                    Status = OrderStatus.Pending,
                    AddressSnap = normalizedAddressSnap,
                    CreatedAt = now,
                    Items = orderItems
                });
            }
        }

        _orders.AddRange(orderList.Where(o => o.Id == 0).ToList()); // Only add NEW orders
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

    public async Task<(bool Success, string? Error, OrderViewResponse? Data)> GetDetailForAdminAsync(long orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdWithItemsAsync(orderId, ct);
        if (order is null) return (false, "Order not found.", null);
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
        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _notifications.Add(_notificationFactory.OrderConfirmed(order.SellerId, order.Id));

        await _uow.SaveChangesAsync(ct);
        return (true, null, new FulfillmentActionResponse(order.Id, order.Status.ToString()));
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(long userId, long orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null) return (false, "Order not found.");
        if (order.UserId != userId) return (false, "Forbidden.");

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Canceled)
            return (false, "Only pending or canceled orders can be deleted.");

        _orders.Remove(order);
        await _uow.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task FinalizeOrderPayoutAsync(Order order, DateTimeOffset now, CancellationToken ct)
    {
        var commission = await _commissions.GetByOrderIdAsync(order.Id, ct);
        var amount = commission?.SellerNetAmount ?? order.Total;

        _escrows.AddRange(new[]
        {
            new EscrowTransaction
            {
                OrderId = order.Id,
                SellerId = order.SellerId,
                Type = EscrowType.OUT,
                Amount = amount,
                CreatedAt = now
            }
        });

        var existing = await _payouts.GetByOrderIdAsync(order.Id, ct);
        if (existing is null)
        {
            _payouts.Add(new Payout
            {
                OrderId = order.Id,
                SellerId = order.SellerId,
                Amount = amount,
                Status = PayoutStatus.Pending,
                CreatedAt = now
            });
        }
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
            o.PaymentOrders.OrderByDescending(po => po.PaymentId).Select(po => (long?)po.PaymentId).FirstOrDefault(),
            o.AddressSnap,
            o.Carrier,
            o.TrackingNumber,
            o.CreatedAt,
            o.DeliveredAt,
            o.User is null ? null : new OrderUserSummary(o.User.Id, o.User.Username, o.User.FullName, o.User.AvatarUrl),
            o.Seller is null ? null : new OrderSellerSummary(o.Seller.Id, o.Seller.Username, o.Seller.FullName, o.Seller.AvatarUrl, o.Seller.AvgSellerRating, o.Seller.TotalRatings),
            o.Dispute is null ? null : new OrderDisputeSummary(o.Dispute.Id, o.Dispute.Status.ToString(), o.Dispute.AdminNote, o.Dispute.ResolvedAt),
            o.Items.Select(i => new OrderItemSummary(i.Id, i.ProductId, i.ProductName, i.ProductImage, i.Quantity, i.UnitPrice, i.Subtotal)).ToList());

    private static string? NormalizeAddressSnap(CheckoutAddressSnap input)
    {
        if (string.IsNullOrWhiteSpace(input.Address)
            || string.IsNullOrWhiteSpace(input.Fullname)
            || string.IsNullOrWhiteSpace(input.City)
            || string.IsNullOrWhiteSpace(input.Phone)
            || string.IsNullOrWhiteSpace(input.Zip))
            return null;

        return JsonSerializer.Serialize(input);
    }
}
