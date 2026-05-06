using System.Text.Json;
using EzBias.Application.Features.Checkout.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Checkout;

public class CheckoutApplicationService : ICheckoutApplicationService
{
    private readonly ICartRepository _cartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _uow;

    public CheckoutApplicationService(ICartRepository cartRepository, IOrderRepository orderRepository, IPaymentRepository paymentRepository, IUnitOfWork uow)
    {
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, CheckoutPreviewResponse? Data)> PreviewAsync(long userId, CheckoutPreviewRequest request, CancellationToken ct)
    {
        if (request.CartItemIds is null || request.CartItemIds.Count == 0)
            return (false, "Please select at least one cart item.", null);

        var items = await _cartRepository.GetByUserIdAndIdsAsync(userId, request.CartItemIds.Distinct().ToList(), ct);
        if (items.Count == 0)
            return (false, "No cart items found.", null);

        foreach (var item in items)
        {
            if (item.Product.DeletedAt is not null || item.Product.Status != ProductStatus.Active || item.Product.IsAuction)
                return (false, $"Product '{item.Product.Name}' is not available for checkout.", null);

            if (item.Product.Stock < item.Quantity)
                return (false, $"Product '{item.Product.Name}' does not have enough stock.", null);
        }

        var mappedItems = items.Select(x => new CheckoutItemDto(
            x.Id,
            x.ProductId,
            x.Product.Name,
            x.Product.PrimaryImageUrl,
            x.Quantity,
            x.Product.Price,
            x.Product.Price * x.Quantity,
            x.Product.SellerId)).ToList();

        var sellerGroups = mappedItems
            .GroupBy(x => x.SellerId)
            .Select(g =>
            {
                var subtotal = g.Sum(x => x.Subtotal);
                return new CheckoutSellerGroupDto(g.Key, g.ToList(), subtotal, 0, subtotal);
            })
            .ToList();

        var itemsTotal = sellerGroups.Sum(x => x.Subtotal);
        var shippingTotal = sellerGroups.Sum(x => x.ShippingFee);

        return (true, null, new CheckoutPreviewResponse(
            sellerGroups,
            itemsTotal,
            shippingTotal,
            itemsTotal + shippingTotal));
    }

    public async Task<(bool Success, string? Error, CheckoutSubmitResponse? Data)> SubmitAsync(long userId, CheckoutSubmitRequest request, CancellationToken ct)
    {
        var preview = await PreviewAsync(userId, new CheckoutPreviewRequest(request.CartItemIds), ct);
        if (!preview.Success || preview.Data is null)
            return (false, preview.Error, null);

        var normalizedAddressSnap = NormalizeAddressSnap(request.AddressSnap);
        if (normalizedAddressSnap is null)
            return (false, "addressSnap must be valid JSON or plain text.", null);

        var cartItems = await _cartRepository.GetByUserIdAndIdsAsync(userId, request.CartItemIds.Distinct().ToList(), ct);
        var now = DateTimeOffset.UtcNow;
        var orderList = new List<Order>();

        foreach (var sellerGroup in preview.Data.Sellers)
        {
            var order = new Order
            {
                UserId = userId,
                SellerId = sellerGroup.SellerId,
                Source = OrderSource.Cart,
                ShippingFee = request.ShippingFee,
                Total = sellerGroup.Total + request.ShippingFee,
                Status = OrderStatus.Pending,
                AddressSnap = normalizedAddressSnap,
                CreatedAt = now,
                Items = sellerGroup.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImage = i.ProductImage,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal
                }).ToList()
            };

            orderList.Add(order);
        }

        _orderRepository.AddRange(orderList);

        var payment = new Payment
        {
            UserId = userId,
            Type = PaymentType.Order,
            Amount = orderList.Sum(x => x.Total),
            Currency = "VND",
            Status = PaymentStatus.Pending,
            Reference = $"PAY-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{Guid.NewGuid():N}"[..24],
            CreatedAt = now,
            PaymentOrders = orderList.Select(x => new PaymentOrder
            {
                Order = x
            }).ToList()
        };

        _paymentRepository.Add(payment);
        _cartRepository.RemoveRange(cartItems);

        await _uow.SaveChangesAsync(ct);

        return (true, null, new CheckoutSubmitResponse(
            payment.Id,
            payment.Reference,
            payment.Amount,
            orderList.Select(x => x.Id).ToList()));
    }

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
