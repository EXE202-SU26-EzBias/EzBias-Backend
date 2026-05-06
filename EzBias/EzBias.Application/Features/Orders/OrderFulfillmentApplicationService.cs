using EzBias.Application.Features.Orders.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Orders;

public class OrderFulfillmentApplicationService : IOrderFulfillmentApplicationService
{
    private readonly IOrderRepository _orders;
    private readonly IEscrowRepository _escrows;
    private readonly IUnitOfWork _uow;

    public OrderFulfillmentApplicationService(IOrderRepository orders, IEscrowRepository escrows, IUnitOfWork uow)
    {
        _orders = orders;
        _escrows = escrows;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, FulfillmentActionResponse? Data)> MarkShippedAsync(long sellerId, long orderId, MarkShippedRequest request, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.SellerId != sellerId) return (false, "Forbidden.", null);
        if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Processing)
            return (false, "Order cannot be marked shipped in current status.", null);

        order.Carrier = request.Carrier?.Trim();
        order.TrackingNumber = request.TrackingNumber?.Trim();
        order.ShippedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Shipped;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return (true, null, new FulfillmentActionResponse(order.Id, order.Status.ToString()));
    }

    public async Task<(bool Success, string? Error, FulfillmentActionResponse? Data)> ConfirmReceivedAsync(long buyerId, long orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.UserId != buyerId) return (false, "Forbidden.", null);
        if (order.Status != OrderStatus.Shipped && order.Status != OrderStatus.Delivered)
            return (false, "Order cannot be confirmed in current status.", null);

        order.DeliveredAt = DateTimeOffset.UtcNow;
        order.CompletedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _escrows.AddRange(new[]
        {
            new EscrowTransaction
            {
                OrderId = order.Id,
                SellerId = order.SellerId,
                Type = EscrowType.OUT,
                Amount = order.Total,
                CreatedAt = DateTimeOffset.UtcNow
            }
        });

        await _uow.SaveChangesAsync(ct);
        return (true, null, new FulfillmentActionResponse(order.Id, order.Status.ToString()));
    }
}
