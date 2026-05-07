using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Products.Dtos;

public record ProductItemResponse(long Id, long SellerId, string FandomId, string Artist, string Name, string Type, ProductCondition Condition, decimal Price, int Stock, string Description, string PrimaryImageUrl, bool IsAuction, ProductStatus Status, DateTimeOffset CreatedAt);
public record CreateProductRequest(string FandomId, string Artist, string Name, string Type, ProductCondition Condition, decimal Price, int Stock, string Description, string PrimaryImageUrl);
public record UpdateProductRequest(decimal Price, int Stock, string Description, ProductStatus Status, string PrimaryImageUrl);
