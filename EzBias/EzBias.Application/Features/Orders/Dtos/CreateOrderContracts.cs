namespace EzBias.Application.Features.Orders.Dtos;

public record CreateOrderRequest(CheckoutAddressSnap AddressSnap, IReadOnlyList<CheckoutCartItemRequest> Items);
public record CheckoutAddressSnap(string Address, string Fullname, string City, string Phone, string Zip);
public record CheckoutCartItemRequest(long CartItemId, int Quantity);
public record CreateOrderResponse(IReadOnlyList<long> OrderIds);
