using EzBias.Application.Common.Results;
using EzBias.Application.Features.Products.Dtos;

namespace EzBias.Application.Features.Products;

public interface IProductManagementApplicationService
{
    Task<IReadOnlyList<ProductItemResponse>> GetMineAsync(long sellerId, CancellationToken ct);
    Task<Result<ProductItemResponse>> GetMineByIdAsync(long sellerId, long productId, CancellationToken ct);
    Task<Result<ProductItemResponse>> CreateAsync(long sellerId, CreateProductRequest request, CancellationToken ct);
    Task<Result<ProductItemResponse>> UpdateAsync(long sellerId, long productId, UpdateProductRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(long sellerId, long productId, CancellationToken ct);
}
