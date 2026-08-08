using EzBias.Application.Features.Deposits;
using EzBias.Application.Common.Results;
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
    private readonly IAuctionDepositRepository _auctionDeposits;

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
        IDepositApplicationService deposits,
        IAuctionDepositRepository auctionDeposits)
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
        _auctionDeposits = auctionDeposits;
    }

    public async Task<Result<CreatePaymentResponse>> CreateAsync(long userId, CreatePaymentRequest request, CancellationToken ct)
    {
        if (request.OrderIds is null || request.OrderIds.Count == 0) return Result<CreatePaymentResponse>.Fail("OrderIds is required.", ApplicationErrorCode.Validation);

        var uniqueOrderIds = request.OrderIds.Distinct().ToList();
        var orders = new List<Order>();
        foreach (var orderId in uniqueOrderIds)
        {
            var order = await _orders.GetByIdAsync(orderId, ct);
            if (order is null) return Result<CreatePaymentResponse>.Fail($"Order {orderId} not found.", ApplicationErrorCode.ResourceNotFound);
            if (order.UserId != userId) return Result<CreatePaymentResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
            if (order.Status != OrderStatus.Pending) return Result<CreatePaymentResponse>.Fail($"Order {orderId} is not pending.", ApplicationErrorCode.Validation);
            orders.Add(order);
        }

        var alreadyHasPayment = false;
        foreach (var id in uniqueOrderIds)
            alreadyHasPayment = alreadyHasPayment || await _payments.ExistsByOrderIdAsync(id, ct);
        if (alreadyHasPayment) return Result<CreatePaymentResponse>.Fail("One or more orders already has payment.", ApplicationErrorCode.Validation);

        var amount = orders.Sum(x => x.Total);
        var auctionOrder = orders[0];
        if (uniqueOrderIds.Count == 1 && auctionOrder.Source == OrderSource.Auction && auctionOrder.AuctionId is long winnerAuctionId)
        {
            var amountDueResult = await _deposits.ComputeWinnerAmountDueAsync(
                winnerAuctionId, userId, auctionOrder.Total, ct);
            if (amountDueResult.IsSuccess)
                amount = amountDueResult.Value;
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

        return Result<CreatePaymentResponse>.Ok(new CreatePaymentResponse(payment.Id, payment.Reference, payment.Amount, payment.Status.ToString()));
    }

    public async Task<Result<PaymentStatusResponse>> GetStatusAsync(long userId, long paymentId, CancellationToken ct)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return Result<PaymentStatusResponse>.Fail("Payment not found.", ApplicationErrorCode.ResourceNotFound);
        if (payment.UserId != userId) return Result<PaymentStatusResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var orderIds = payment.PaymentOrders.Select(po => po.OrderId).ToList();
        var orders = payment.PaymentOrders
            .Select(po => new PaymentOrderSummary(po.OrderId, po.Order.Total, po.Order.Status, po.Order.UserId, po.Order.SellerId))
            .ToList();

        return Result<PaymentStatusResponse>.Ok(new PaymentStatusResponse(payment.Id, payment.Reference, payment.Amount, payment.Status.ToString(), payment.CreatedAt, payment.PaidAt, orderIds, orders));
    }

    public async Task<Result> ConfirmAuctionDepositAsync(long userId, long auctionId, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null)
            return Result.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);

        var deposit = await _auctionDeposits.GetLatestByUserAndAuctionAsync(userId, auctionId, ct);
        if (deposit is null)
            return Result.Fail("Deposit not found for this auction.", ApplicationErrorCode.ResourceNotFound);

        if (deposit.State == DepositState.Held)
            return Result.Ok();

        if (deposit.State != DepositState.PendingPayment)
            return Result.Fail("Deposit is not awaiting payment.", ApplicationErrorCode.Validation);

        if (deposit.PaymentId is not long paymentId)
            return Result.Fail("Deposit has no linked payment.", ApplicationErrorCode.Validation);

        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return Result.Fail("Deposit payment not found.", ApplicationErrorCode.ResourceNotFound);

        if (payment.UserId != userId || payment.Type != PaymentType.AuctionDeposit)
            return Result.Fail("Deposit payment is invalid.", ApplicationErrorCode.Validation);

        if (payment.Amount != deposit.Amount)
            return Result.Fail("Deposit payment amount is invalid.", ApplicationErrorCode.Validation);

        if (payment.Status == PaymentStatus.Paid)
            return await _deposits.ConfirmDepositAsync(payment.Id, ct);

        if (payment.Status != PaymentStatus.Pending)
            return Result.Fail("Deposit payment cannot be confirmed in its current state.", ApplicationErrorCode.Validation);

        return await ConfirmBySePayPullAsync(payment, ct);
    }

    public async Task<Result> ConfirmManualAsync(long adminId, long paymentId, CancellationToken ct)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return Result.Fail("Payment not found.", ApplicationErrorCode.ResourceNotFound);

        payment.ProviderTxnId ??= $"MANUAL-{adminId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        payment.Payload = $"{{\"source\":\"manual-admin-confirm\",\"adminId\":{adminId},\"at\":\"{DateTimeOffset.UtcNow:O}\"}}";
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        return await ConfirmInternalAsync(payment.UserId, payment.Id, ct);
    }

    public async Task<Result> HandleWebhookAsync(PaymentWebhookRequest request, string rawBody, string? signature, string? timestamp, CancellationToken ct)
    {
        if (!_webhookVerifier.Verify(rawBody, signature, timestamp))
            return Result.Fail("Invalid webhook signature.", ApplicationErrorCode.InvalidWebhookSignature);

        var payment = await _payments.GetByReferenceAsync(request.Reference, ct);
        if (payment is null) return Result.Fail("Payment not found.", ApplicationErrorCode.ResourceNotFound);
        if (payment.Status == PaymentStatus.Paid) return Result.Ok();

        return await ConfirmBySePayPullAsync(payment, ct);
    }

    public async Task<Result> HandleSePayWebhookAsync(SePayWebhookPayload payload, string rawBody, string? signature, string? timestamp, CancellationToken ct)
    {
        if (!_webhookVerifier.Verify(rawBody, signature, timestamp))
            return Result.Fail("Invalid webhook signature.", ApplicationErrorCode.InvalidWebhookSignature);

        if (payload.TransferType is not null && !payload.TransferType.Equals("in", StringComparison.OrdinalIgnoreCase))
            return Result.Fail("Unsupported transfer type.", ApplicationErrorCode.Validation);

        var content = payload.Content ?? payload.Description ?? string.Empty;
        var reference = ExtractReference(content);
        if (string.IsNullOrWhiteSpace(reference))
            return Result.Fail("Reference not found in SePay content.", ApplicationErrorCode.Validation);

        var mapped = new PaymentWebhookRequest(reference, payload.Id?.ToString() ?? payload.ReferenceCode, content, rawBody);
        var result = await HandleWebhookAsync(mapped, rawBody, signature, timestamp, ct);
        if (!result.IsSuccess && result.Failure?.Code == ApplicationErrorCode.ResourceNotFound)
            return Result.Fail(result.Failure.Message, ApplicationErrorCode.Validation);

        return result;
    }

    private async Task<Result> ConfirmBySePayPullAsync(Payment payment, CancellationToken ct)
    {
        var pull = await _sepay.GetTransactionsAsync(ct);
        if (!pull.Success)
        {
            var msg = pull.RetryAfterSeconds.HasValue ? $"{pull.Error} Retry after {pull.RetryAfterSeconds.Value}s." : pull.Error;
            return Result.Fail(msg ?? "SePay transaction lookup failed.", ApplicationErrorCode.Validation);
        }

        var matched = pull.Transactions.FirstOrDefault(x =>
            Math.Abs(x.AmountIn - payment.Amount) < 0.01m &&
            (ContainsNormalized(x.TransactionContent, payment.Reference) ||
             ContainsNormalized(x.TransactionContent, payment.TransferContent)));

        if (matched is null) return Result.Fail("No matching SePay transaction found.", ApplicationErrorCode.Validation);

        payment.ProviderTxnId = matched.Id;
        payment.Payload = System.Text.Json.JsonSerializer.Serialize(matched);
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        var confirm = await ConfirmInternalAsync(payment.UserId, payment.Id, ct);
        if (!confirm.IsSuccess)
            return Result.Fail(
                confirm.Failure
                ?? ApplicationError.Create(
                    ApplicationErrorCode.Validation,
                    "Payment confirmation failed."));

        return Result.Ok();
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

    private async Task<Result> ConfirmInternalAsync(long userId, long paymentId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var payment = await _payments.GetByIdWithOrdersForUpdateAsync(paymentId, ct);
        if (payment is null)
            return Result.Fail("Payment not found.", ApplicationErrorCode.ResourceNotFound);

        if (payment.UserId != userId)
            return Result.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        if (payment.Status == PaymentStatus.Paid)
        {
            return Result.Ok();
        }

        if (payment.MarkPaid(now) == TransitionOutcome.Invalid)
            return Result.Fail("Payment cannot be confirmed in current status.", ApplicationErrorCode.Validation);

        if (payment.Type == PaymentType.AuctionDeposit)
        {
            await _uow.SaveChangesAsync(ct);
            var hold = await _deposits.ConfirmDepositAsync(payment.Id, ct);
            if (!hold.IsSuccess)
                return Result.Fail(
                    hold.Failure
                    ?? ApplicationError.Create(
                        ApplicationErrorCode.Validation,
                        "Deposit could not be held."));
            await transaction.CommitAsync(ct);
            return Result.Ok();
        }

        foreach (var po in payment.PaymentOrders)
        {
            if (po.Order.MarkPaid(now) == TransitionOutcome.Invalid)
                return Result.Fail("Order cannot be marked paid in current status.", ApplicationErrorCode.Validation);

            var productNames = string.Join(", ", po.Order.Items.Select(i => i.ProductName));
            _notifications.Add(_notificationFactory.OrderPlaced(po.Order.SellerId, po.OrderId, productNames));

            if (po.Order.Source == OrderSource.Auction && po.Order.AuctionId.HasValue)
            {
                var auction = await _auctions.GetByIdAsync(po.Order.AuctionId.Value, ct);
                if (auction is not null && auction.Status == AuctionStatus.EndedPendingPayment)
                {
                    if (auction.MarkSold(now) == TransitionOutcome.Invalid)
                        return Result.Fail("Auction cannot be marked sold in current status.", ApplicationErrorCode.Validation);

                    var product = await _products.GetByIdAsync(auction.ProductId, ct);
                    if (product is not null)
                    {
                        product.IsAuction = false;
                        product.UpdatedAt = DateTimeOffset.UtcNow;
                    }

                    await _deposits.ApplyWinnerDepositAsync(po.Order.AuctionId.Value, po.Order.UserId, ct);
                }
                continue;
            }

            foreach (var item in po.Order.Items)
            {
                if (item.Product is null)
                    return Result.Fail(
                        $"Product {item.ProductId} no longer exists.",
                        ApplicationErrorCode.ResourceNotFound);

                if (item.Product.Stock < item.Quantity)
                    return Result.Fail($"Insufficient stock for product {item.ProductId}.", ApplicationErrorCode.Validation);

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
        return Result.Ok();
    }
}
