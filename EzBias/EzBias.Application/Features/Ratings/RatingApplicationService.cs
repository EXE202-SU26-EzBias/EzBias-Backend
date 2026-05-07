using EzBias.Application.Features.Ratings.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Ratings;

public class RatingApplicationService : IRatingApplicationService
{
    private readonly IRatingRepository _ratings;
    private readonly IOrderRepository _orders;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public RatingApplicationService(IRatingRepository ratings, IOrderRepository orders, IUserRepository users, IUnitOfWork uow)
    {
        _ratings = ratings;
        _orders = orders;
        _users = users;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, RatingResponse? Data)> CreateAsync(long buyerId, CreateRatingRequest request, CancellationToken ct)
    {
        if (request.ProductRating < 1 || request.ProductRating > 5 || request.SellerRating < 1 || request.SellerRating > 5)
            return (false, "Rating must be between 1 and 5.", null);

        var order = await _orders.GetByIdAsync(request.OrderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.UserId != buyerId) return (false, "Forbidden.", null);
        if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.Delivered)
            return (false, "Order is not eligible for rating.", null);

        var exists = await _ratings.ExistsByOrderIdAsync(order.Id, ct);
        if (exists) return (false, "Order already rated.", null);

        var rating = new Rating
        {
            OrderId = order.Id,
            BuyerId = buyerId,
            SellerId = order.SellerId,
            ProductRating = request.ProductRating,
            SellerRating = request.SellerRating,
            Tags = request.Tags ?? Array.Empty<string>(),
            Comment = request.Comment,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _ratings.Add(rating);

        var seller = await _users.GetByIdAsync(order.SellerId, ct);
        if (seller is not null)
        {
            seller.TotalRatings += 1;
            seller.AvgSellerRating = ((seller.AvgSellerRating * (seller.TotalRatings - 1)) + request.SellerRating) / seller.TotalRatings;
            seller.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(rating));
    }

    public async Task<IReadOnlyList<RatingResponse>> GetBySellerAsync(long sellerId, CancellationToken ct)
        => (await _ratings.GetBySellerIdAsync(sellerId, ct)).Select(Map).ToList();

    private static RatingResponse Map(Rating x) => new(x.Id, x.OrderId, x.BuyerId, x.SellerId, x.ProductRating, x.SellerRating, x.Tags, x.Comment, x.CreatedAt);
}
