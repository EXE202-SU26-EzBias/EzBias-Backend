using EzBias.Application.Features.Deposits;
using EzBias.Application.Features.Notifications;
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
    private readonly IProductRepository _products;
    private readonly IEscrowRepository _escrows;
    private readonly ICommissionRepository _commissions;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly ISePayClient _sepay;
    private readonly IUnitOfWork _uow;
    private readonly ISePayWebhookVerifier _webhookVerifier;
    private readonly ICommissionRateProvider _commissionRateProvider;
    private readonly IDepositApplicationService _deposits;

    public PaymentApplicationService(
        IPaymentRepository payments,
        IOrderRepository orders,
        IAuctionRepository auctions,
        IProductRepository products,
        IEscrowRepository escrows,
        ICommissionRepository commissions,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        ISePayClient sepay,
        IUnitOfWork uow,
        ISePayWebhookVerifier webhookVerifier,
        ICommissionRateProvider commissionRateProvider,
        IDepositApplicationService deposits)
    {
        _payments = payments;
        _orders = orders;
        _auctions = auctions;
        _products = products;
        _escrows = escrows;
        _commissions = commissions;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _sepay = sepay;
        _uow = uow;
        _webhookVerifier = webhookVerifier;
        _commissionRateProvider = commissionRateProvider;
        _deposits = deposits;
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
        var auctionOrder = orders[0];
        if (uniqueOrderIds.Count == 1 && auctionOrder.Source == OrderSource.Auction && auctionOrder.AuctionId is long winnerAuctionId)
        {
            var (computed, _, amountDue) = await _deposits.ComputeWinnerAmountDueAsync(
                winnerAuctionId, userId, auctionOrder.Total, ct);
            if (computed) amount = amountDue; // Req 6.2/6.4: reduce by held deposit; Order.Total stays = FinalPrice
        }
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

        return (true, null, new CreatePaymentResponse(payment.Id, payment.Reference, payment.Amount, payment.Status.ToString()));
    }

    public async Task<(bool Success, string? Error, PaymentStatusResponse? Data)> GetStatusAsync(long userId, long paymentId, CancellationToken ct)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return (false, "Payment not found.", null);
        if (payment.UserId != userId) return (false, "Forbidden.", null);

        var orderIds = payment.PaymentOrders.Select(po => po.OrderId).ToList();
        var orders = payment.PaymentOrders
            .Select(po => new PaymentOrderSummary(po.OrderId, po.Order.Total, po.Order.Status, po.Order.UserId, po.Order.SellerId))
            .ToList();

        return (true, null, new PaymentStatusResponse(payment.Id, payment.Reference, payment.Amount, payment.Status.ToString(), payment.CreatedAt, payment.PaidAt, orderIds, orders));
    }

    public async Task<(bool Success, string? Error)> ConfirmManualAsync(long adminId, long paymentId, CancellationToken ct)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return (false, "Payment not found.");

        payment.ProviderTxnId ??= $"MANUAL-{adminId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        payment.Payload = $"{{\"source\":\"manual-admin-confirm\",\"adminId\":{adminId},\"at\":\"{DateTimeOffset.UtcNow:O}\"}}";
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        return await ConfirmInternalAsync(payment.UserId, payment.Id, ct);
    }

    public async Task<(bool Success, string? Error)> HandleWebhookAsync(PaymentWebhookRequest request, string rawBody, string? signature, string? timestamp, CancellationToken ct)
    {
        if (!_webhookVerifier.Verify(rawBody, signature, timestamp))
            return (false, "Invalid webhook signature.");

        var payment = await _payments.GetByReferenceAsync(request.Reference, ct);
        if (payment is null) return (false, "Payment not found.");
        if (payment.Status == PaymentStatus.Paid) return (true, null);

        return await ConfirmBySePayPullAsync(payment, ct);
    }

    public async Task<(bool Success, string? Error)> HandleSePayWebhookAsync(SePayWebhookPayload payload, string rawBody, string? signature, string? timestamp, CancellationToken ct)
    {
        if (!_webhookVerifier.Verify(rawBody, signature, timestamp))
            return (false, "Invalid webhook signature.");

        if (payload.TransferType is not null && !payload.TransferType.Equals("in", StringComparison.OrdinalIgnoreCase))
            return (false, "Unsupported transfer type.");

        var content = payload.Content ?? payload.Description ?? string.Empty;
        var reference = ExtractReference(content);
        if (string.IsNullOrWhiteSpace(reference))
            return (false, "Reference not found in SePay content.");

        var mapped = new PaymentWebhookRequest(reference, payload.Id?.ToString() ?? payload.ReferenceCode, content, rawBody);
        return await HandleWebhookAsync(mapped, rawBody, signature, timestamp, ct);
    }

    private async Task<(bool Success, string? Error)> ConfirmBySePayPullAsync(Payment payment, CancellationToken ct)
    {
        var pull = await _sepay.GetTransactionsAsync(ct);
        if (!pull.Success)
        {
            var msg = pull.RetryAfterSeconds.HasValue ? $"{pull.Error} Retry after {pull.RetryAfterSeconds.Value}s." : pull.Error;
            return (false, msg);
        }

        var matched = pull.Transactions.FirstOrDefault(x =>
            Math.Abs(x.AmountIn - payment.Amount) < 0.01m &&
            (ContainsNormalized(x.TransactionContent, payment.Reference) ||
             ContainsNormalized(x.TransactionContent, payment.TransferContent)));

        if (matched is null) return (false, "No matching SePay transaction found.");

        payment.ProviderTxnId = matched.Id;
        payment.Payload = System.Text.Json.JsonSerializer.Serialize(matched);
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        var confirm = await ConfirmInternalAsync(payment.UserId, payment.Id, ct);
        if (!confirm.Success) return (false, confirm.Error);

        return (true, null);
    }

    private static string Normalize(string? input)
        => new string((input ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static bool ContainsNormalized(string? haystack, string? needle)
    {
        var h = Normalize(haystack);
        var n = Normalize(needle);
        return !string.IsNullOrWhiteSpace(n) && h.Contains(n, StringComparison.Ordinal);
    }

    private static string? ExtractReference(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var normalized = Normalize(content);

        var marker = "PAY";
        var idx = normalized.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;

        var tail = normalized[(idx + marker.Length)..];
        var digits = new string(tail.TakeWhile(char.IsDigit).ToArray());
        if (digits.Length < 15) return null;

        var ts = digits[..14];
        var userId = digits[14..];
        if (string.IsNullOrWhiteSpace(userId)) return null;

        return $"PAY-{ts}-{userId}";
    }

    private async Task<(bool Success, string? Error)> ConfirmInternalAsync(long userId, long paymentId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var payment = await _payments.GetByIdWithOrdersForUpdateAsync(paymentId, ct);
        if (payment is null)
            return (false, "Payment not found.");

        if (payment.UserId != userId)
            return (false, "Forbidden.");

        if (payment.Status == PaymentStatus.Paid)
        {
            return (true, null);
        }

        if (payment.MarkPaid(now) == TransitionOutcome.Invalid)
            return (false, "Payment cannot be confirmed in current status.");

        if (payment.Type == PaymentType.AuctionDeposit)
        {
            // Deposit payments have no orders/escrow/commission. Persist the Paid status, then hand off
            // to the Deposit_Service to transition the linked deposit PendingPayment -> Held (Req 3.1).
            await _uow.SaveChangesAsync(ct);
            var hold = await _deposits.ConfirmDepositAsync(payment.Id, ct);
            if (!hold.Success) return (false, hold.Error);
            await transaction.CommitAsync(ct);
            return (true, null);
        }

        foreach (var po in payment.PaymentOrders)
        {
            if (po.Order.MarkPaid(now) == TransitionOutcome.Invalid)
                return (false, "Order cannot be marked paid in current status.");

            // Notify seller of new order
            var productNames = string.Join(", ", po.Order.Items.Select(i => i.ProductName));
            _notifications.Add(_notificationFactory.OrderPlaced(po.Order.SellerId, po.OrderId, productNames));

            if (po.Order.Source == OrderSource.Auction && po.Order.AuctionId.HasValue)
            {
                var auction = await _auctions.GetByIdAsync(po.Order.AuctionId.Value, ct);
                if (auction is not null && auction.Status == AuctionStatus.EndedPendingPayment)
                {
                    if (auction.MarkSold(now) == TransitionOutcome.Invalid)
                        return (false, "Auction cannot be marked sold in current status.");
                    
                    // Product was successfully sold in auction, free it up
                    var product = await _products.GetByIdAsync(auction.ProductId, ct);
                    if (product is not null)
                    {
                        product.IsAuction = false;
                        product.UpdatedAt = DateTimeOffset.UtcNow;
                    }

                    // Req 6.3: the winner's Held deposit is consumed toward the final payment (Held -> Applied).
                    // Ignore the result: the winner has already paid, so a missing/already-applied deposit
                    // must not block marking the order Paid.
                    await _deposits.ApplyWinnerDepositAsync(po.Order.AuctionId.Value, po.Order.UserId, ct);
                }
                continue;
            }

            foreach (var item in po.Order.Items)
            {
                if (item.Product is null)
                    return (false, $"Product {item.ProductId} no longer exists.");

                if (item.Product.Stock < item.Quantity)
                    return (false, $"Insufficient stock for product {item.ProductId}.");

                item.Product.Stock -= item.Quantity;
                item.Product.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        var hasHold = await _escrows.ExistsHoldByPaymentIdAsync(payment.Id, ct);
        var hasCommission = await _commissions.ExistsByPaymentIdAsync(payment.Id, ct);

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
        }

        if (!hasCommission)
        {
            var ratePercent = _commissionRateProvider.GetRatePercent();
            var commissionTransactions = payment.PaymentOrders.Select(po =>
            {
                var commissionAmount = Math.Round(po.Order.Total * ratePercent / 100m, 2, MidpointRounding.AwayFromZero);
                return new CommissionTransaction
                {
                    OrderId = po.OrderId,
                    PaymentId = payment.Id,
                    SellerId = po.Order.SellerId,
                    GrossAmount = po.Order.Total,
                    CommissionRatePercent = ratePercent,
                    CommissionAmount = commissionAmount,
                    SellerNetAmount = po.Order.Total - commissionAmount,
                    Currency = payment.Currency,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }).ToList();

            _commissions.AddRange(commissionTransactions);
        }

        await _uow.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return (true, null);
    }
}
