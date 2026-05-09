namespace EzBias.Application.Features.Products.Dtos;

public record CatalogProductItem(long Id, long SellerId, string FandomId, string Artist, string Name, string Type, decimal Price, int Stock, string PrimaryImageUrl, bool IsAuction, EzBias.Domain.Enums.ProductStatus Status, DateTimeOffset CreatedAt);
public record CatalogFandomItem(string Id, string Name, bool IsActive);
