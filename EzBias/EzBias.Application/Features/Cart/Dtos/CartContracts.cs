namespace EzBias.Application.Features.Cart.Dtos;

public record UpsertCartItemRequest(long ProductId, int Quantity);
public record UpdateCartItemQuantityRequest(int Quantity);
public record CartItemDto(long CartItemId, long ProductId, string ProductName, string ProductImage, decimal UnitPrice, int Quantity, decimal Subtotal, long SellerId);
public record CartResponse(IReadOnlyList<CartItemDto> Items, decimal Total);
