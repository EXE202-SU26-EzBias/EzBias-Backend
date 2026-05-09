using EzBias.Application.Features.Orders.Dtos;

namespace EzBias.Application.Features.Orders;

public interface IOrderApplicationService
{
    Task<(bool Success, string? Error, CreateOrderResponse? Data)> CreateAsync(long userId, CreateOrderRequest request, CancellationToken ct);
    Task<IReadOnlyList<OrderViewResponse>> GetByBuyerAsync(long userId, CancellationToken ct);
    Task<(bool Success, string? Error, OrderViewResponse? Data)> GetDetailAsync(long userId, long orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderViewResponse>> GetBySellerAsync(long sellerId, CancellationToken ct);
    Task<(bool Success, string? Error, FulfillmentActionResponse? Data)> ConfirmReceivedAsync(long userId, long orderId, CancellationToken ct);
}
