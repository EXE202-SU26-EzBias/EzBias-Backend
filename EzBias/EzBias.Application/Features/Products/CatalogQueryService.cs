using EzBias.Application.Features.Products.Dtos;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Products;

public class CatalogQueryService : ICatalogQueryService
{
    private readonly IProductRepository _products;
    private readonly IFandomRepository _fandoms;

    public CatalogQueryService(IProductRepository products, IFandomRepository fandoms)
    {
        _products = products;
        _fandoms = fandoms;
    }

    public async Task<IReadOnlyList<CatalogProductItem>> GetProductsAsync(string? fandomId, CancellationToken ct)
    {
        var items = await _products.GetActiveAsync(fandomId, ct);
        return items.Select(x => new CatalogProductItem(x.Id, x.SellerId, x.FandomId, x.Artist, x.Name, x.Type, x.Price, x.Stock, x.PrimaryImageUrl, x.IsAuction, x.Status, x.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<CatalogFandomItem>> GetFandomsAsync(CancellationToken ct)
    {
        var items = await _fandoms.GetActiveAsync(ct);
        return items.Select(x => new CatalogFandomItem(x.Id, x.Name, x.IsActive)).ToList();
    }
}
