using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Products.Dtos;

public record ProductItemResponse(long Id, long SellerId, string FandomId, string Artist, string Name, string Type, ProductCondition Condition, decimal Price, int Stock, string Description, string PrimaryImageUrl, IReadOnlyList<string> ImageUrls, bool IsAuction, ProductStatus Status, DateTimeOffset CreatedAt);
public record CreateProductRequest(string FandomId, string Artist, string Name, string Type, ProductCondition Condition, decimal Price, int Stock, string Description, string PrimaryImageUrl, IReadOnlyList<string>? ImageUrls = null);
public record UpdateProductRequest(decimal Price, int Stock, string Description, ProductStatus Status, string PrimaryImageUrl);
