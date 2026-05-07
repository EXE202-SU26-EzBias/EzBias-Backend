namespace EzBias.Application.Features.Ratings.Dtos;

public record CreateRatingRequest(long OrderId, short ProductRating, short SellerRating, string[]? Tags, string? Comment);
public record RatingResponse(long Id, long OrderId, long BuyerId, long SellerId, short ProductRating, short SellerRating, string[] Tags, string? Comment, DateTimeOffset CreatedAt);
