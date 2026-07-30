using EzBias.Application.Common.Results;
using EzBias.Application.Features.Cart.Dtos;

namespace EzBias.Application.Features.Cart;

public interface ICartApplicationService
{
    Task<Result> UpsertItemAsync(long userId, UpsertCartItemRequest request, CancellationToken ct);
    Task<CartResponse> GetMyCartAsync(long userId, CancellationToken ct);
    Task<Result> UpdateItemQuantityAsync(long userId, long cartItemId, UpdateCartItemQuantityRequest request, CancellationToken ct);
    Task<Result> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct);
    Task<Result> AddAuctionItemToCartAsync(long userId, long auctionId, CancellationToken ct);
}
