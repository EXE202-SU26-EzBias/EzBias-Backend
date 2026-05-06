namespace EzBias.Application.Features.Checkout.Dtos;

public record CheckoutPreviewRequest(IReadOnlyList<long> CartItemIds);
public record CheckoutSubmitRequest(IReadOnlyList<long> CartItemIds, decimal ShippingFee = 0, string? AddressSnap = null);

public record CheckoutItemDto(long CartItemId, long ProductId, string ProductName, string ProductImage, int Quantity, decimal UnitPrice, decimal Subtotal, long SellerId);
public record CheckoutSellerGroupDto(long SellerId, IReadOnlyList<CheckoutItemDto> Items, decimal Subtotal, decimal ShippingFee, decimal Total);

public record CheckoutPreviewResponse(IReadOnlyList<CheckoutSellerGroupDto> Sellers, decimal ItemsTotal, decimal ShippingTotal, decimal GrandTotal);

public record CheckoutSubmitResponse(long PaymentId, string PaymentReference, decimal Amount, IReadOnlyList<long> OrderIds);
