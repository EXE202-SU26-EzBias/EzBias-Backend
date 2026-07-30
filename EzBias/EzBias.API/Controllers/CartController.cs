using System.Security.Claims;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Cart;
using EzBias.Application.Features.Cart.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartApplicationService _cartService;

    public CartController(ICartApplicationService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCart(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _cartService.GetMyCartAsync(userId, ct);
        return Ok(data);
    }

    [HttpPost("items")]
    public async Task<IActionResult> UpsertItem([FromBody] UpsertCartItemRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _cartService.UpsertItemAsync(userId, request, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, notFoundAsBadRequest: true);

        return Ok(new { message = "Cart updated." });
    }

    [HttpPost("auction/{auctionId:long}")]
    public async Task<IActionResult> AddAuctionItemToCart([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _cartService.AddAuctionItemToCartAsync(userId, auctionId, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, notFoundAsBadRequest: true);

        return Ok(new { message = "Auction item added to cart." });
    }

    [HttpPut("items/{cartItemId:long}/quantity")]
    public async Task<IActionResult> UpdateItemQuantity(
        [FromRoute] long cartItemId,
        [FromBody] UpdateCartItemQuantityRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _cartService.UpdateItemQuantityAsync(userId, cartItemId, request, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result);

        return Ok(new { message = "Cart item quantity updated." });
    }

    [HttpDelete("items/{cartItemId:long}")]
    public async Task<IActionResult> RemoveItem([FromRoute] long cartItemId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _cartService.RemoveItemAsync(userId, cartItemId, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, forceKind: ErrorKind.NotFound);

        return NoContent();
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
