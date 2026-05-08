namespace EzBias.Application.Features.Orders.Dtos;

public record CreateOrderRequest(IReadOnlyList<long> CartItemIds, string? AddressSnap = null);
public record CreateOrderResponse(IReadOnlyList<long> OrderIds);
