using EzBias.Application.Features.Products.Dtos;

namespace EzBias.Application.Features.Products;

public interface ICatalogQueryService
{
    Task<IReadOnlyList<CatalogProductItem>> GetProductsAsync(string? fandomId, CancellationToken ct);
    Task<IReadOnlyList<CatalogFandomItem>> GetFandomsAsync(CancellationToken ct);
}
