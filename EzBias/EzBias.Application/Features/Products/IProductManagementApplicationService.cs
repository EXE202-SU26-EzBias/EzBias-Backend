using EzBias.Application.Features.Products.Dtos;

namespace EzBias.Application.Features.Products;

public interface IProductManagementApplicationService
{
    Task<IReadOnlyList<ProductItemResponse>> GetMineAsync(long sellerId, CancellationToken ct);
    Task<(bool Success, string? Error, ProductItemResponse? Data)> GetMineByIdAsync(long sellerId, long productId, CancellationToken ct);
    Task<(bool Success, string? Error, ProductItemResponse? Data)> CreateAsync(long sellerId, CreateProductRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, ProductItemResponse? Data)> UpdateAsync(long sellerId, long productId, UpdateProductRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> DeleteAsync(long sellerId, long productId, CancellationToken ct);
}
