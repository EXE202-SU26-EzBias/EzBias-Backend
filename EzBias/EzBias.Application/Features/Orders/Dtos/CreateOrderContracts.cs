namespace EzBias.Application.Features.Orders.Dtos;

public record CreateOrderRequest(string? AddressSnap, IReadOnlyList<CheckoutCartItemRequest> Items);
public record CheckoutCartItemRequest(long CartItemId, int Quantity);
public record CreateOrderResponse(IReadOnlyList<long> OrderIds);
