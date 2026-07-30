using EzBias.Application.Common.Results;
using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Entities;

namespace EzBias.Application.Features.Orders;

public interface IOrderApplicationService
{
    Task<Result<CreateOrderResponse>> CreateAsync(long userId, CreateOrderRequest request, CancellationToken ct);
    Task<IReadOnlyList<OrderViewResponse>> GetByBuyerAsync(long userId, CancellationToken ct);
    Task<Result<OrderViewResponse>> GetDetailAsync(long userId, long orderId, CancellationToken ct);
    Task<Result<OrderViewResponse>> GetDetailForAdminAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderViewResponse>> GetBySellerAsync(long sellerId, CancellationToken ct);
    Task<Result<FulfillmentActionResponse>> MarkShippedAsync(
        long sellerId,
        long orderId,
        string? carrier,
        CancellationToken ct);
    Task<Result<FulfillmentActionResponse>> ConfirmReceivedAsync(long userId, long orderId, CancellationToken ct);
    Task<Result> DeleteAsync(long userId, long orderId, CancellationToken ct);
    Task FinalizeOrderPayoutAsync(Order order, DateTimeOffset now, CancellationToken ct);
}
