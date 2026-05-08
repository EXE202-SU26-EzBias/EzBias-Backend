using EzBias.Application.Features.Payments.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Payments;

public class PaymentApplicationService : IPaymentApplicationService
{
    private readonly IPaymentRepository _payments;
    private readonly IOrderRepository _orders;
    private readonly IAuctionRepository _auctions;
    private readonly IEscrowRepository _escrows;
    private readonly ISePayClient _sepay;
    private readonly IUnitOfWork _uow;

    public PaymentApplicationService(IPaymentRepository payments, IOrderRepository orders, IAuctionRepository auctions, IEscrowRepository escrows, ISePayClient sepay, IUnitOfWork uow)
    {
        _payments = payments;
        _orders = orders;
        _auctions = auctions;
        _escrows = escrows;
        _sepay = sepay;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, CreatePaymentResponse? Data)> CreateAsync(long userId, CreatePaymentRequest request, CancellationToken ct)
    {
        if (request.OrderIds is null || request.OrderIds.Count == 0) return (false, "OrderIds is required.", null);

        var uniqueOrderIds = request.OrderIds.Distinct().ToList();
        var orders = new List<Order>();
        foreach (var orderId in uniqueOrderIds)
        {
            var order = await _orders.GetByIdAsync(orderId, ct);
            if (order is null) return (false, $"Order {orderId} not found.", null);
            if (order.UserId != userId) return (false, "Forbidden.", null);
            if (order.Status != OrderStatus.Pending) return (false, $"Order {orderId} is not pending.", null);
            orders.Add(order);
        }

        var alreadyHasPayment = false;
        foreach (var id in uniqueOrderIds)
            alreadyHasPayment = alreadyHasPayment || await _payments.ExistsByOrderIdAsync(id, ct);
        if (alreadyHasPayment) return (false, "One or more orders already has payment.", null);

        var amount = orders.Sum(x => x.Total);
        var now = DateTimeOffset.UtcNow;
        var reference = $"PAY-{now:yyyyMMddHHmmss}-{userId}";
        var transfer = $"EZB-{userId}-{now:HHmmss}";

        var payment = new Payment
        {
            UserId = userId,
            Type = PaymentType.Order,
            Amount = amount,
            Currency = "VND",
            Status = PaymentStatus.Pending,
            Reference = reference,
            TransferContent = transfer,
            Payload = "{}",
            CreatedAt = now,
            PaymentOrders = uniqueOrderIds.Select(id => new PaymentOrder { OrderId = id }).ToList()
        };

        _payments.Add(payment);
        await _uow.SaveChangesAsync(ct);

        return (true, null, new CreatePaymentResponse(payment.Id, payment.Reference, payment.Amount, payment.Status.ToString(), payment.TransferContent ?? string.Empty));
    }

    public async Task<(bool Success, string? Error, PaymentStatusResponse? Data)> GetStatusAsync(long userId, long paymentId, CancellationToken ct)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return (false, "Payment not found.", null);
        if (payment.UserId != userId) return (false, "Forbidden.", null);
        return (true, null, new PaymentStatusResponse(payment.Id, payment.Reference, payment.Amount, payment.Status.ToString(), payment.CreatedAt, payment.PaidAt));
    }

    public async Task<(bool Success, string? Error)> HandleWebhookAsync(PaymentWebhookRequest request, CancellationToken ct)
    {
        var payment = await _payments.GetByReferenceAsync(request.Reference, ct);
        if (payment is null) return (false, "Payment not found.");
        if (payment.Status == PaymentStatus.Paid) return (true, null);

        var pull = await _sepay.GetTransactionsAsync("050134288091", 200, ct);
        if (!pull.Success)
        {
            var msg = pull.RetryAfterSeconds.HasValue ? $"{pull.Error} Retry after {pull.RetryAfterSeconds.Value}s." : pull.Error;
            return (false, msg);
        }

        var keys = new List<string>();
        if (!string.IsNullOrWhiteSpace(payment.Reference)) keys.Add(payment.Reference);
        if (!string.IsNullOrWhiteSpace(payment.TransferContent)) keys.Add(payment.TransferContent!);

        var matched = pull.Transactions.FirstOrDefault(x =>
            Math.Abs(x.AmountIn - payment.Amount) < 0.01m &&
            !string.IsNullOrWhiteSpace(x.TransactionContent) &&
            keys.Any(k => x.TransactionContent.Contains(k, StringComparison.OrdinalIgnoreCase)));

        if (matched is null) return (false, "No matching SePay transaction found.");

        payment.ProviderTxnId = matched.Id;
        payment.Payload = System.Text.Json.JsonSerializer.Serialize(matched);
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        var confirm = await ConfirmInternalAsync(payment.UserId, payment.Id, ct);
        if (!confirm.Success) return (false, confirm.Error);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> MarkPaidManualAsync(long userId, long paymentId, CancellationToken ct)
        => await ConfirmInternalAsync(userId, paymentId, ct);

    private async Task<(bool Success, string? Error)> ConfirmInternalAsync(long userId, long paymentId, CancellationToken ct)
    {
        var payment = await _payments.GetByIdWithOrdersAsync(paymentId, ct);
        if (payment is null)
            return (false, "Payment not found.");

        if (payment.UserId != userId)
            return (false, "Forbidden.");

        if (payment.Status == PaymentStatus.Paid)
        {
            return (true, null);
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
                    return (false, $"Insufficient stock for product {item.ProductId}.");

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

        return (true, null);
    }
}
