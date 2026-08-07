using System.Security.Cryptography;
using System.Text;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Products.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Exceptions;
using EzBias.Domain.Interfaces;
using EzBias.Domain.Services;

namespace EzBias.Application.Features.Products;

public class ProductManagementApplicationService : IProductManagementApplicationService
{
    private readonly IProductRepository _products;
    private readonly IFandomRepository _fandoms;
    private readonly IUnitOfWork _uow;

    public ProductManagementApplicationService(IProductRepository products, IFandomRepository fandoms, IUnitOfWork uow)
    {
        _products = products;
        _fandoms = fandoms;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ProductItemResponse>> GetMineAsync(long sellerId, CancellationToken ct)
        => (await _products.GetBySellerAsync(sellerId, ct)).Select(Map).ToList();

    public async Task<Result<ProductItemResponse>> GetMineByIdAsync(long sellerId, long productId, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(productId, ct);
        if (p is null || p.DeletedAt is not null) return Result<ProductItemResponse>.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);
        if (p.SellerId != sellerId) return Result<ProductItemResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        return Result<ProductItemResponse>.Ok(Map(p));
    }

    public async Task<Result<ProductItemResponse>> CreateAsync(long sellerId, CreateProductRequest request, CancellationToken ct)
    {
        if (request.Price <= 0) return Result<ProductItemResponse>.Fail("Price must be greater than zero.", ApplicationErrorCode.Validation);
        if (request.Stock < 0) return Result<ProductItemResponse>.Fail("Stock cannot be negative.", ApplicationErrorCode.Validation);
        var imageUrls = NormalizeImageUrls(request.ImageUrls, request.PrimaryImageUrl);
        if (imageUrls.Count == 0) return Result<ProductItemResponse>.Fail("At least one product image is required.", ApplicationErrorCode.Validation);

        var fandomResult = await ResolveFandomAsync(request.FandomName, request.LegacyFandomId, ct);
        if (fandomResult.Fandom is null)
            return Result<ProductItemResponse>.Fail(
                fandomResult.Error ?? "Fandom could not be resolved.", ApplicationErrorCode.Validation);

        var p = new Product
        {
            SellerId = sellerId,
            FandomId = fandomResult.Fandom.Id,
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

        var pendingFandom = fandomResult.WasAdded ? fandomResult.Fandom : null;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _uow.SaveChangesAsync(ct);
                break;
            }
            catch (FandomWriteConflictException) when (pendingFandom is not null && attempt < 2)
            {
                _fandoms.Detach(pendingFandom);

                fandomResult = await ResolveFandomAsync(request.FandomName, request.LegacyFandomId, ct);
                if (fandomResult.Fandom is null)
                    return Result<ProductItemResponse>.Fail(
                        fandomResult.Error ?? "Fandom could not be resolved.", ApplicationErrorCode.Validation);

                p.FandomId = fandomResult.Fandom.Id;
                _products.Add(p);
                pendingFandom = fandomResult.WasAdded ? fandomResult.Fandom : null;
            }
        }

        return Result<ProductItemResponse>.Ok(Map(p));
    }

    public async Task<Result<ProductItemResponse>> UpdateAsync(long sellerId, long productId, UpdateProductRequest request, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(productId, ct);
        if (p is null || p.DeletedAt is not null) return Result<ProductItemResponse>.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);
        if (p.SellerId != sellerId) return Result<ProductItemResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (request.Price <= 0) return Result<ProductItemResponse>.Fail("Price must be greater than zero.", ApplicationErrorCode.Validation);
        if (request.Stock < 0) return Result<ProductItemResponse>.Fail("Stock cannot be negative.", ApplicationErrorCode.Validation);

        p.Price = request.Price;
        p.Stock = request.Stock;
        p.Description = request.Description.Trim();
        p.Status = request.Status;

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

        if (p.Images.Count == 0)
            return Result<ProductItemResponse>.Fail("At least one product image is required.", ApplicationErrorCode.Validation);

        var firstImage = p.Images.OrderBy(x => x.SortOrder).FirstOrDefault();
        if (firstImage is not null)
            p.PrimaryImageUrl = firstImage.Url;

        p.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return Result<ProductItemResponse>.Ok(Map(p));
    }

    public async Task<Result> DeleteAsync(long sellerId, long productId, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(productId, ct);
        if (p is null || p.DeletedAt is not null) return Result.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);
        if (p.SellerId != sellerId) return Result.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        p.DeletedAt = DateTimeOffset.UtcNow;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        p.Status = ProductStatus.Archived;

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
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

    private async Task<(Fandom? Fandom, bool WasAdded, string? Error)> ResolveFandomAsync(
        string? fandomName,
        string? legacyFandomId,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(fandomName))
        {
            if (!FandomNameNormalizer.TryNormalize(fandomName, out var displayName, out var normalizedName, out var error))
                return (null, false, error);

            var existing = await _fandoms.GetByNormalizedNameAsync(normalizedName, ct);
            if (existing is not null)
                return existing.IsActive
                    ? (existing, false, null)
                    : (null, false, "This fandom is not available.");

            var fandom = new Fandom
            {
                Id = await CreateAvailableFandomIdAsync(displayName, normalizedName, ct),
                Name = displayName,
                NormalizedName = normalizedName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _fandoms.Add(fandom);
            return (fandom, true, null);
        }

        if (string.IsNullOrWhiteSpace(legacyFandomId))
            return (null, false, "Fandom is required.");

        var legacyFandom = await _fandoms.GetByIdAsync(legacyFandomId.Trim(), ct);
        if (legacyFandom is null)
            return (null, false, "Fandom not found.");

        return legacyFandom.IsActive
            ? (legacyFandom, false, null)
            : (null, false, "This fandom is not available.");
    }

    private async Task<string> CreateAvailableFandomIdAsync(string displayName, string normalizedName, CancellationToken ct)
    {
        var baseSlug = FandomNameNormalizer.ToSlug(displayName);
        if (await _fandoms.GetByIdAsync(baseSlug, ct) is null)
            return baseSlug;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedName))).ToLowerInvariant();
        foreach (var hashLength in new[] { 8, 16, 32, 64 })
        {
            var candidate = $"{baseSlug}-{hash[..hashLength]}";
            if (await _fandoms.GetByIdAsync(candidate, ct) is null)
                return candidate;
        }

        var suffix = 2;
        while (await _fandoms.GetByIdAsync($"{baseSlug}-{hash}-{suffix}", ct) is not null)
            suffix++;

        return $"{baseSlug}-{hash}-{suffix}";
    }
}
