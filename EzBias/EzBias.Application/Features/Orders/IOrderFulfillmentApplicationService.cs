using EzBias.Application.Features.Orders.Dtos;

namespace EzBias.Application.Features.Orders;

public interface IOrderFulfillmentApplicationService
{
    Task<(bool Success, string? Error, FulfillmentActionResponse? Data)> MarkShippedAsync(long sellerId, long orderId, MarkShippedRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, FulfillmentActionResponse? Data)> ConfirmReceivedAsync(long buyerId, long orderId, CancellationToken ct);
}
