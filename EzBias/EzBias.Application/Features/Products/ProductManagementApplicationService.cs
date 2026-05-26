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
        var imageUrls = NormalizeImageUrls(request.ImageUrls, request.PrimaryImageUrl);
        if (imageUrls.Count == 0) return (false, "At least one product image is required.", null);

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
            PrimaryImageUrl = imageUrls[0],
            Status = ProductStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            Images = imageUrls.Select((url, index) => new ProductImage
            {
                Url = url,
                SortOrder = (short)(index + 1),
                CreatedAt = DateTimeOffset.UtcNow
            }).ToList()
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

        // Remove images not in the keep list (when caller explicitly provides the list)
        if (request.KeepImageUrls is not null)
        {
            var keepSet = new HashSet<string>(request.KeepImageUrls, StringComparer.OrdinalIgnoreCase);
            var toRemove = p.Images.Where(img => !keepSet.Contains(img.Url)).ToList();
            foreach (var img in toRemove)
                p.Images.Remove(img);
        }

        if (request.NewImageUrls is { Count: > 0 })
        {
            var nextOrder = p.Images.Count > 0
                ? (short)(p.Images.Max(x => x.SortOrder) + 1)
                : (short)1;

            foreach (var url in request.NewImageUrls)
            {
                p.Images.Add(new ProductImage
                {
                    Url = url,
                    SortOrder = nextOrder++,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        // Must have at least one image after all changes
        if (p.Images.Count == 0)
            return (false, "At least one product image is required.", null);

        // Recalculate primary: first remaining image wins
        var firstImage = p.Images.OrderBy(x => x.SortOrder).FirstOrDefault();
        if (firstImage is not null)
            p.PrimaryImageUrl = firstImage.Url;

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
    {
        var imageUrls = p.Images
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Url)
            .DefaultIfEmpty(p.PrimaryImageUrl)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return new ProductItemResponse(p.Id, p.SellerId, p.FandomId, p.Artist, p.Name, p.Type, p.Condition, p.Price, p.Stock, p.Description, p.PrimaryImageUrl, imageUrls, p.IsAuction, p.Status, p.CreatedAt);
    }

    private static List<string> NormalizeImageUrls(IReadOnlyList<string>? imageUrls, string? primaryImageUrl)
    {
        var urls = new List<string>();

        if (imageUrls is not null)
            urls.AddRange(imageUrls);

        if (!string.IsNullOrWhiteSpace(primaryImageUrl))
            urls.Insert(0, primaryImageUrl);

        return urls
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }
}
