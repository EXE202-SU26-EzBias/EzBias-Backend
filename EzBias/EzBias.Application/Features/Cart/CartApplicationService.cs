using EzBias.Application.Features.Cart.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Cart;

public class CartApplicationService : ICartApplicationService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CartApplicationService(ICartRepository cartRepository, IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Success, string? Error)> UpsertItemAsync(long userId, UpsertCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
            return (false, "Quantity must be greater than 0.");

        var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
        if (product is null || product.DeletedAt is not null)
            return (false, "Product not found.");

        if (product.Status != ProductStatus.Active)
            return (false, "Product is not available.");

        if (product.IsAuction)
            return (false, "Auction products cannot be added to cart.");

        if (product.SellerId == userId)
            return (false, "You cannot add your own product to cart.");

        if (product.Stock < request.Quantity)
            return (false, "Not enough stock.");

        var existing = await _cartRepository.GetByUserAndProductAsync(userId, request.ProductId, ct);
        if (existing is null)
        {
            _cartRepository.Add(new CartItem
            {
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.Quantity = request.Quantity;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<CartResponse> GetMyCartAsync(long userId, CancellationToken ct)
    {
        var items = await _cartRepository.GetByUserIdAsync(userId, ct);
        var mapped = items.Select(x => new CartItemDto(
            x.Id,
            x.ProductId,
            x.Product.Name,
            x.Product.PrimaryImageUrl,
            x.Product.Price,
            x.Quantity,
            x.Product.Price * x.Quantity,
            x.Product.SellerId)).ToList();

        return new CartResponse(mapped, mapped.Sum(x => x.Subtotal));
    }

    public async Task<(bool Success, string? Error)> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct)
    {
        var item = await _cartRepository.GetByIdAsync(cartItemId, ct);
        if (item is null || item.UserId != userId)
            return (false, "Cart item not found.");

        _cartRepository.Remove(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return (true, null);
    }
}
