using EzBias.Application.Features.Payments.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Payments;

public class PaymentApplicationService : IPaymentApplicationService
{
    private readonly IPaymentRepository _payments;
    private readonly IAuctionRepository _auctions;
    private readonly IEscrowRepository _escrows;
    private readonly IUnitOfWork _uow;

    public PaymentApplicationService(IPaymentRepository payments, IAuctionRepository auctions, IEscrowRepository escrows, IUnitOfWork uow)
    {
        _payments = payments;
        _auctions = auctions;
        _escrows = escrows;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, ConfirmPaymentResponse? Data)> ConfirmAsync(long userId, long paymentId, CancellationToken ct)
    {
        var payment = await _payments.GetByIdWithOrdersAsync(paymentId, ct);
        if (payment is null)
            return (false, "Payment not found.", null);

        if (payment.UserId != userId)
            return (false, "Forbidden.", null);

        var orderIds = payment.PaymentOrders.Select(x => x.OrderId).Distinct().ToList();

        if (payment.Status == PaymentStatus.Paid)
        {
            var existingCount = await _escrows.ExistsHoldByPaymentIdAsync(payment.Id, ct) ? orderIds.Count : 0;
            return (true, null, new ConfirmPaymentResponse(payment.Id, payment.Status.ToString(), orderIds, existingCount));
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTimeOffset.UtcNow;
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var po in payment.PaymentOrders)
        {
            po.Order.Status = OrderStatus.Paid;
            po.Order.UpdatedAt = DateTimeOffset.UtcNow;

            if (po.Order.Source == OrderSource.Auction && po.Order.AuctionId.HasValue)
            {
                var auction = await _auctions.GetByIdAsync(po.Order.AuctionId.Value, ct);
                if (auction is not null && auction.Status == AuctionStatus.EndedPendingPayment)
                {
                    auction.Status = AuctionStatus.Sold;
                    auction.UpdatedAt = DateTimeOffset.UtcNow;
                }
                continue;
            }

            foreach (var item in po.Order.Items)
            {
                if (item.Product.Stock < item.Quantity)
                    return (false, $"Insufficient stock for product {item.ProductId}.", null);

                item.Product.Stock -= item.Quantity;
                item.Product.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        var hasHold = await _escrows.ExistsHoldByPaymentIdAsync(payment.Id, ct);
        var holdCount = 0;

        if (!hasHold)
        {
            var holds = payment.PaymentOrders.Select(po => new EscrowTransaction
            {
                OrderId = po.OrderId,
                SellerId = po.Order.SellerId,
                Type = EscrowType.IN,
                Amount = po.Order.Total,
                PaymentId = payment.Id,
                CreatedAt = DateTimeOffset.UtcNow
            }).ToList();

            _escrows.AddRange(holds);
            holdCount = holds.Count;
        }

        await _uow.SaveChangesAsync(ct);

        return (true, null, new ConfirmPaymentResponse(payment.Id, payment.Status.ToString(), orderIds, holdCount));
    }
}
