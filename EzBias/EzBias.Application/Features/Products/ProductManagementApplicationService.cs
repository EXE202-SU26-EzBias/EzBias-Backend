using EzBias.Application.Features.Products.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Products;

public class ProductManagementApplicationService : IProductManagementApplicationService
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;

    public ProductManagementApplicationService(IProductRepository products, IUnitOfWork uow)
    {
        _products = products;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ProductItemResponse>> GetMineAsync(long sellerId, CancellationToken ct)
        => (await _products.GetBySellerAsync(sellerId, ct)).Select(Map).ToList();

    public async Task<(bool Success, string? Error, ProductItemResponse? Data)> GetMineByIdAsync(long sellerId, long productId, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(productId, ct);
        if (p is null || p.DeletedAt is not null) return (false, "Product not found.", null);
        if (p.SellerId != sellerId) return (false, "Forbidden.", null);
        return (true, null, Map(p));
    }

    public async Task<(bool Success, string? Error, ProductItemResponse? Data)> CreateAsync(long sellerId, CreateProductRequest request, CancellationToken ct)
    {
        if (request.Price <= 0) return (false, "Price must be greater than zero.", null);
        if (request.Stock < 0) return (false, "Stock cannot be negative.", null);

        var p = new Product
        {
            SellerId = sellerId,
            FandomId = request.FandomId.Trim(),
            Artist = request.Artist.Trim(),
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Condition = request.Condition,
            Price = request.Price,
            Stock = request.Stock,
            Description = request.Description.Trim(),
            PrimaryImageUrl = request.PrimaryImageUrl.Trim(),
            Status = ProductStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _products.Add(p);
        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(p));
    }

    public async Task<(bool Success, string? Error, ProductItemResponse? Data)> UpdateAsync(long sellerId, long productId, UpdateProductRequest request, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(productId, ct);
        if (p is null || p.DeletedAt is not null) return (false, "Product not found.", null);
        if (p.SellerId != sellerId) return (false, "Forbidden.", null);
        if (request.Price <= 0) return (false, "Price must be greater than zero.", null);
        if (request.Stock < 0) return (false, "Stock cannot be negative.", null);

        p.Price = request.Price;
        p.Stock = request.Stock;
        p.Description = request.Description.Trim();
        p.Status = request.Status;
        p.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(p));
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(long sellerId, long productId, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(productId, ct);
        if (p is null || p.DeletedAt is not null) return (false, "Product not found.");
        if (p.SellerId != sellerId) return (false, "Forbidden.");

        p.DeletedAt = DateTimeOffset.UtcNow;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        p.Status = ProductStatus.Archived;

        await _uow.SaveChangesAsync(ct);
        return (true, null);
    }

    private static ProductItemResponse Map(Product p)
        => new(p.Id, p.SellerId, p.FandomId, p.Artist, p.Name, p.Type, p.Condition, p.Price, p.Stock, p.Description, p.PrimaryImageUrl, p.IsAuction, p.Status, p.CreatedAt);
}
