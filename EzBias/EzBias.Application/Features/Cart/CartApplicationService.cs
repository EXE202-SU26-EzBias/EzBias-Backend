using EzBias.Application.Common.Results;
using EzBias.Application.Features.Cart.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Cart;

public class CartApplicationService : ICartApplicationService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CartApplicationService(ICartRepository cartRepository, IProductRepository productRepository, IAuctionRepository auctionRepository, IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _auctionRepository = auctionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> UpsertItemAsync(long userId, UpsertCartItemRequest request, CancellationToken ct)
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
        
        var mapped = new List<CartItemDto>();
        foreach (var item in items)
        {
            decimal unitPrice = item.Product.Price;
            
            // Check if this product is from a won auction
            var auction = await _auctionRepository.GetByProductIdAndWinnerAsync(item.ProductId, userId, ct);
            if (auction is not null && auction.Status == AuctionStatus.EndedPendingPayment && auction.FinalPrice.HasValue)
            {
                // Use the final bid price instead of product price
                unitPrice = auction.FinalPrice.Value;
            }
            
            mapped.Add(new CartItemDto(
                item.Id,
                item.ProductId,
                item.Product.Name,
                item.Product.PrimaryImageUrl,
                unitPrice,
                item.Quantity,
                unitPrice * item.Quantity,
                item.Product.SellerId));
        }

        return new CartResponse(mapped, mapped.Sum(x => x.Subtotal));
    }

    public async Task<Result> UpdateItemQuantityAsync(
        long userId,
        long cartItemId,
        UpdateCartItemQuantityRequest request,
        CancellationToken ct)
    {
        if (request.Quantity <= 0)
            return (false, "Quantity must be greater than 0.");

        var item = await _cartRepository.GetByIdAsync(cartItemId, ct);
        if (item is null || item.UserId != userId)
            return (false, "Cart item not found.");

        var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
        if (product is null || product.DeletedAt is not null)
            return (false, "Product not found.");

        if (product.Status != ProductStatus.Active)
            return (false, "Product is not available.");

        if (product.Stock < request.Quantity)
            return (false, "Not enough stock.");

        item.Quantity = request.Quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<Result> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct)
    {
        var item = await _cartRepository.GetByIdAsync(cartItemId, ct);
        if (item is null || item.UserId != userId)
            return (false, "Cart item not found.");

        _cartRepository.Remove(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<Result> AddAuctionItemToCartAsync(long userId, long auctionId, CancellationToken ct)
    {
        var auction = await _auctionRepository.GetByIdAsync(auctionId, ct);
        if (auction is null)
            return (false, "Auction not found.");

        if (auction.Status != AuctionStatus.EndedPendingPayment)
            return (false, "Auction is not in pending payment status.");

        if (auction.WinnerId != userId)
            return (false, "You are not the winner of this auction.");

        var product = await _productRepository.GetByIdAsync(auction.ProductId, ct);
        if (product is null || product.DeletedAt is not null)
            return (false, "Product not found.");

        // Check if auction item already in cart
        var existing = await _cartRepository.GetByUserAndProductAsync(userId, product.Id, ct);
        if (existing is not null)
            return (true, null); // Already in cart, no error

        // Add auction product to cart with quantity 1
        _cartRepository.Add(new CartItem
        {
            UserId = userId,
            ProductId = product.Id,
            Quantity = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        return (true, null);
    }
}
