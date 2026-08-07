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
            return Result.Fail("Quantity must be greater than 0.", ApplicationErrorCode.Validation);

        var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
        if (product is null || product.DeletedAt is not null)
            return Result.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);

        if (product.Status != ProductStatus.Active)
            return Result.Fail("Product is not available.", ApplicationErrorCode.Validation);

        if (product.IsAuction)
            return Result.Fail("Auction products cannot be added to cart.", ApplicationErrorCode.Validation);

        if (product.SellerId == userId)
            return Result.Fail("You cannot add your own product to cart.", ApplicationErrorCode.Validation);

        if (product.Stock < request.Quantity)
            return Result.Fail("Not enough stock.", ApplicationErrorCode.Validation);

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
        return Result.Ok();
    }

    public async Task<CartResponse> GetMyCartAsync(long userId, CancellationToken ct)
    {
        var items = await _cartRepository.GetByUserIdAsync(userId, ct);
        
        var mapped = new List<CartItemDto>();
        foreach (var item in items)
        {
            decimal unitPrice = item.Product.Price;

            var auction = await _auctionRepository.GetByProductIdAndWinnerAsync(item.ProductId, userId, ct);
            if (auction is not null && auction.Status == AuctionStatus.EndedPendingPayment && auction.FinalPrice.HasValue)
            {
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
            return Result.Fail("Quantity must be greater than 0.", ApplicationErrorCode.Validation);

        var item = await _cartRepository.GetByIdAsync(cartItemId, ct);
        if (item is null || item.UserId != userId)
            return Result.Fail("Cart item not found.", ApplicationErrorCode.ResourceNotFound);

        var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
        if (product is null || product.DeletedAt is not null)
            return Result.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);

        if (product.Status != ProductStatus.Active)
            return Result.Fail("Product is not available.", ApplicationErrorCode.Validation);

        if (product.Stock < request.Quantity)
            return Result.Fail("Not enough stock.", ApplicationErrorCode.Validation);

        item.Quantity = request.Quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct)
    {
        var item = await _cartRepository.GetByIdAsync(cartItemId, ct);
        if (item is null || item.UserId != userId)
            return Result.Fail("Cart item not found.", ApplicationErrorCode.ResourceNotFound);

        _cartRepository.Remove(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> AddAuctionItemToCartAsync(long userId, long auctionId, CancellationToken ct)
    {
        var auction = await _auctionRepository.GetByIdAsync(auctionId, ct);
        if (auction is null)
            return Result.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);

        if (auction.Status != AuctionStatus.EndedPendingPayment)
            return Result.Fail("Auction is not in pending payment status.", ApplicationErrorCode.Validation);

        if (auction.WinnerId != userId)
            return Result.Fail("You are not the winner of this auction.", ApplicationErrorCode.Validation);

        var product = await _productRepository.GetByIdAsync(auction.ProductId, ct);
        if (product is null || product.DeletedAt is not null)
            return Result.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);

        var existing = await _cartRepository.GetByUserAndProductAsync(userId, product.Id, ct);
        if (existing is not null)
            return Result.Ok();

        _cartRepository.Add(new CartItem
        {
            UserId = userId,
            ProductId = product.Id,
            Quantity = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
