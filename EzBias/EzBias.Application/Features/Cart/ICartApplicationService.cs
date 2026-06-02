using EzBias.Application.Features.Cart.Dtos;

namespace EzBias.Application.Features.Cart;

public interface ICartApplicationService
{
    Task<(bool Success, string? Error)> UpsertItemAsync(long userId, UpsertCartItemRequest request, CancellationToken ct);
    Task<CartResponse> GetMyCartAsync(long userId, CancellationToken ct);
    Task<(bool Success, string? Error)> UpdateItemQuantityAsync(long userId, long cartItemId, UpdateCartItemQuantityRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct);
    Task<(bool Success, string? Error)> AddAuctionItemToCartAsync(long userId, long auctionId, CancellationToken ct);
}
