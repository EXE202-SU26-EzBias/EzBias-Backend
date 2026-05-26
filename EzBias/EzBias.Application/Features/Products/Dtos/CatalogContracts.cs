using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Products.Dtos;

public record CatalogProductItem(long Id, long SellerId, string FandomId, string Artist, string Name, string Type, decimal Price, int Stock, string PrimaryImageUrl, bool IsAuction, ProductStatus Status, DateTimeOffset CreatedAt);

public record CatalogProductDetail(
    long Id,
    long SellerId,
    string FandomId,
    string Artist,
    string Name,
    string Type,
    ProductCondition Condition,
    decimal Price,
    int Stock,
    string Description,
    string PrimaryImageUrl,
    IReadOnlyList<string> ImageUrls,
    bool IsAuction,
    ProductStatus Status,
    DateTimeOffset CreatedAt);

public record CatalogFandomItem(string Id, string Name, bool IsActive);
