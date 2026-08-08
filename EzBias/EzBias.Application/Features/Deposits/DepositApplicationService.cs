using EzBias.Application.Common.Results;
using EzBias.Application.Features.Deposits.Dtos;
using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Exceptions;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Deposits;

public class DepositApplicationService : IDepositApplicationService
{
    private const string TerminalStateError = "Deposit is in a terminal state.";
    private const string IllegalTransitionError = "Invalid deposit state transition.";
    private const string ConcurrencyError = "Deposit was modified concurrently; transition rejected.";

    private readonly IAuctionDepositRepository _deposits;
    private readonly IAuctionRepository _auctions;
    private readonly IPaymentRepository _payments;
    private readonly IRefundRepository _refunds;
    private readonly IUserRepository _users;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;

    public DepositApplicationService(
        IAuctionDepositRepository deposits,
        IAuctionRepository auctions,
        IPaymentRepository payments,
        IRefundRepository refunds,
        IUserRepository users,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow)
    {
        _deposits = deposits;
        _auctions = auctions;
        _payments = payments;
        _refunds = refunds;
        _users = users;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
    }

    private static (bool Success, string? Error) TryTransition(AuctionDeposit deposit, DepositState to)
        => TryTransition(deposit, to, out _);

    private static (bool Success, string? Error) TryTransition(
        AuctionDeposit deposit, DepositState to, out TransitionOutcome outcome)
    {
        outcome = deposit.TryTransitionTo(to, DateTimeOffset.UtcNow);
        return outcome switch
        {
            TransitionOutcome.Applied => (true, null),
            TransitionOutcome.Terminal => (false, TerminalStateError),
            _ => (false, IllegalTransitionError)
        };
    }

    private async Task<(bool Ok, string? Error)> SaveAsync(CancellationToken ct)
    {
        try
        {
            await _uow.SaveChangesAsync(ct);
            return (true, null);
        }
        catch (ConcurrencyConflictException)
        {
            return (false, ConcurrencyError);
        }
    }

    public async Task<Result<InitiateDepositResponse>> InitiateDepositAsync(
        long userId, long auctionId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null)
        {
            return Result<InitiateDepositResponse>.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);
        }

        if (auction.Status is not AuctionStatus.Live and not AuctionStatus.Extended)
        {
            return Result<InitiateDepositResponse>.Fail("Auction is not open for deposits.", ApplicationErrorCode.Validation);
        }

        if (auction.SellerId == userId)
        {
            return Result<InitiateDepositResponse>.Fail("A seller cannot deposit on an owned auction.", ApplicationErrorCode.Validation);
        }

        var existing = await _deposits.GetActiveByUserAndAuctionAsync(userId, auctionId, ct);
        if (existing is not null)
        {
            Payment? existingPayment = null;
            if (existing.PaymentId is long existingPaymentId)
            {
                existingPayment = await _payments.GetByIdAsync(existingPaymentId, ct);
            }

            var existingResponse = new InitiateDepositResponse(
                existing.Id,
                auctionId,
                existingPayment?.Id,
                existing.State.ToString(),
                existingPayment?.Reference ?? string.Empty,
                existingPayment?.TransferContent ?? string.Empty,
                existing.Amount,
                existingPayment?.Currency ?? "VND");

            return Result<InitiateDepositResponse>.Ok(existingResponse);
        }

        var now = DateTimeOffset.UtcNow;

        var payment = new Payment
        {
            UserId = userId,
            Type = PaymentType.AuctionDeposit,
            Amount = auction.RequiredDepositAmount,
            Currency = "VND",
            Status = PaymentStatus.Pending,
            Reference = $"PAY-{now:yyyyMMddHHmmss}-{userId}",
            TransferContent = $"EZB-{userId}-{now:HHmmss}",
            Payload = "{}",
            CreatedAt = now
        };
        _payments.Add(payment);

        var deposit = new AuctionDeposit
        {
            AuctionId = auctionId,
            UserId = userId,
            Amount = auction.RequiredDepositAmount,
            State = DepositState.PendingPayment,
            CreatedAt = now,
            Payment = payment
        };
        _deposits.Add(deposit);

        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return Result<InitiateDepositResponse>.Fail(
                saveError ?? "Deposit could not be saved.",
                ApplicationErrorCode.ConcurrencyConflict);
        }

        var adminIds = await _users.GetUserIdsByRoleAsync(UserRole.Admin, ct);
        if (adminIds.Count > 0)
        {
            _notifications.AddRange(adminIds.Select(adminId =>
                _notificationFactory.DepositPendingReview(adminId, deposit.Id, auctionId, deposit.Amount)));
            await _uow.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        var response = new InitiateDepositResponse(
            deposit.Id,
            auctionId,
            payment.Id,
            deposit.State.ToString(),
            payment.Reference,
            payment.TransferContent!,
            payment.Amount,
            payment.Currency);

        return Result<InitiateDepositResponse>.Ok(response);
    }

    public async Task<Result<DepositStatusResponse>> GetMyDepositStatusAsync(
        long userId, long auctionId, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null)
        {
            return Result<DepositStatusResponse>.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);
        }

        var deposit = await _deposits.GetLatestByUserAndAuctionAsync(userId, auctionId, ct);

        if (deposit is null)
        {
            return Result<DepositStatusResponse>.Ok(new DepositStatusResponse(
                auctionId,
                auction.RequiredDepositAmount,
                HasDeposit: false,
                DepositId: null,
                Amount: null,
                State: null,
                PaymentReference: null));
        }

        string? reference = null;
        if (deposit.PaymentId is long pid)
        {
            var p = await _payments.GetByIdAsync(pid, ct);
            reference = p?.Reference;
        }

        return Result<DepositStatusResponse>.Ok(new DepositStatusResponse(
            auctionId,
            auction.RequiredDepositAmount,
            HasDeposit: true,
            DepositId: deposit.Id,
            Amount: deposit.Amount,
            State: deposit.State.ToString(),
            PaymentReference: reference));
    }

    public async Task<Result> ConfirmDepositAsync(long paymentId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var deposit = await _deposits.GetByPaymentIdAsync(paymentId, ct);
        if (deposit is null)
        {
            return Result.Fail("Deposit not found for payment.", ApplicationErrorCode.ResourceNotFound);
        }

        if (deposit.State == DepositState.Held)
        {
            return Result.Ok();
        }

        if (deposit.State != DepositState.PendingPayment)
        {
            deposit.LastError = "Deposit not in PendingPayment state at confirmation.";
            var (savedFail, saveFailError) = await SaveAsync(ct);
            if (!savedFail)
            {
                return Result.Fail(
                    saveFailError ?? "Deposit failure state could not be saved.",
                    ApplicationErrorCode.ConcurrencyConflict);
            }

            return Result.Fail("Deposit is not awaiting payment.", ApplicationErrorCode.Validation);
        }

        var payment = await _payments.GetByIdAsync(paymentId, ct);

        deposit.Amount = payment?.Amount ?? deposit.Amount;
        deposit.HeldAt = DateTimeOffset.UtcNow;

        var (transitioned, transitionError) = TryTransition(deposit, DepositState.Held);
        if (!transitioned)
        {
            return Result.Fail(
                transitionError ?? "Deposit cannot be held.", ApplicationErrorCode.Validation);
        }

        var productName = await ResolveProductNameAsync(deposit.AuctionId, ct);
        _notifications.Add(_notificationFactory.DepositConfirmed(
            deposit.UserId, deposit.AuctionId, productName, deposit.Amount));

        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return Result.Fail(
                saveError ?? "Deposit could not be saved.",
                ApplicationErrorCode.ConcurrencyConflict);
        }

        await transaction.CommitAsync(ct);
        return Result.Ok();
    }

    private async Task<string> ResolveProductNameAsync(long auctionId, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdWithProductAsync(auctionId, ct);
        return auction?.Product?.Name ?? "the auction item";
    }

    private async Task<Result> RefundHeldDepositsAsync(
        long auctionId, long? excludeUserId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var held = await _deposits.GetHeldByAuctionAsync(auctionId, excludeUserId, ct);

        if (held.Count == 0)
        {
            return Result.Ok();
        }

        var productName = await ResolveProductNameAsync(auctionId, ct);

        foreach (var deposit in held)
        {
            try
            {
                if (deposit.PaymentId is null)
                {
                    deposit.LastError = "Cannot refund: deposit has no linked payment.";
                    continue;
                }

                var refund = new Refund
                {
                    PaymentId = deposit.PaymentId.Value,
                    Amount = deposit.Amount,
                    Reason = "Auction deposit refund.",
                    Status = RefundStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _refunds.Add(refund);

                var (ok, err) = TryTransition(deposit, DepositState.Refunded);
                if (!ok)
                {
                    deposit.LastError = err;
                    continue;
                }

                deposit.RefundedAt = DateTimeOffset.UtcNow;
                deposit.Refund = refund;

                _notifications.Add(_notificationFactory.DepositRefundInitiated(
                    deposit.UserId, auctionId, productName, deposit.Amount));
            }
            catch (Exception ex)
            {
                deposit.LastError = $"Refund failed: {ex.Message}";
            }
        }

        var saved = await SaveAsync(ct);
        if (saved.Ok)
            await transaction.CommitAsync(ct);

        return saved.Ok
            ? Result.Ok()
            : Result.Fail(
                saved.Error ?? "Deposit refunds could not be saved.",
                ApplicationErrorCode.ConcurrencyConflict);
    }

    public async Task<Result> ApplyWinnerDepositAsync(
        long auctionId, long winnerId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var deposit = await _deposits.GetHeldByUserAndAuctionAsync(winnerId, auctionId, ct);

        if (deposit is null)
        {
            return Result.Fail("No held deposit available to apply.", ApplicationErrorCode.Validation);
        }

        deposit.AppliedAt = DateTimeOffset.UtcNow;
        var (ok, err) = TryTransition(deposit, DepositState.Applied);
        if (!ok)
        {
            return Result.Fail(
                err ?? "Deposit cannot be applied.", ApplicationErrorCode.Validation);
        }

        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return Result.Fail(
                saveError ?? "Deposit could not be saved.",
                ApplicationErrorCode.ConcurrencyConflict);
        }

        await transaction.CommitAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<decimal>> ComputeWinnerAmountDueAsync(
        long auctionId, long winnerId, decimal finalPrice, CancellationToken ct)
    {
        var deposit = await _deposits.GetHeldByUserAndAuctionAsync(winnerId, auctionId, ct);

        if (deposit is null)
        {
            return Result<decimal>.Fail("No held deposit available to apply.", ApplicationErrorCode.Validation);
        }

        var held = deposit.Amount;

        if (held >= finalPrice)
        {
            return Result<decimal>.Ok(0m);
        }

        var amountDue = finalPrice - held;
        return Result<decimal>.Ok(amountDue);
    }

    public async Task<Result> ForfeitWinnerDepositAsync(
        long auctionId, long winnerId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var deposit = await _deposits.GetHeldByUserAndAuctionAsync(winnerId, auctionId, ct);

        if (deposit is null)
        {
            return Result.Ok();
        }

        var productName = await ResolveProductNameAsync(auctionId, ct);

        deposit.ForfeitedAt = DateTimeOffset.UtcNow;

        var (ok, err) = TryTransition(deposit, DepositState.Forfeited);
        if (!ok)
        {
            deposit.LastError = err;
            return Result.Fail(
                err ?? "Deposit cannot be forfeited.", ApplicationErrorCode.Validation);
        }

        _notifications.Add(_notificationFactory.DepositForfeited(
            deposit.UserId, auctionId, productName, deposit.Amount));

        string? lastSaveError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var (saved, saveError) = await SaveAsync(ct);
            if (saved)
            {
                await transaction.CommitAsync(ct);
                return Result.Ok();
            }

            deposit.ForfeitRetryCount = attempt;
            lastSaveError = saveError;
        }

        deposit.LastError = "Forfeiture did not complete after 3 attempts.";
        await SaveAsync(ct);
        return Result.Fail(lastSaveError ?? "Forfeiture did not complete after 3 attempts.", ApplicationErrorCode.Validation);
    }

    public async Task<Result> ReleaseDepositsOnCancelAsync(long auctionId, CancellationToken ct)
        => await RefundHeldDepositsAsync(auctionId, null, ct);

    public async Task<Result<IReadOnlyList<AdminDepositListItem>>> GetPendingRefundsAsync(
        CancellationToken ct)
    {
        var deposits = await _deposits.GetAllHeldDepositsForAdminAsync(ct);

        var items = deposits.Select(d => new AdminDepositListItem(
            d.Id,
            d.AuctionId,
            d.Auction?.Product?.Name ?? "Unknown Auction",
            d.UserId,
            d.User?.Email ?? "Unknown",
            d.User?.FullName ?? "Unknown",
            d.Amount,
            d.HeldAt ?? d.CreatedAt,
            d.Payment?.Reference,
            d.State.ToString()
        )).ToList();

        return Result<IReadOnlyList<AdminDepositListItem>>.Ok(items);
    }

    public async Task<Result<AdminDepositDetailResponse>> GetDepositDetailAsync(
        long depositId, CancellationToken ct)
    {
        var deposit = await _deposits.GetByIdAsync(depositId, ct);
        if (deposit is null)
        {
            return Result<AdminDepositDetailResponse>.Fail("Deposit not found.", ApplicationErrorCode.ResourceNotFound);
        }

        var auction = await _auctions.GetByIdWithProductAsync(deposit.AuctionId, ct);
        var user = await _users.GetByIdAsync(deposit.UserId, ct);
        Payment? payment = null;
        if (deposit.PaymentId is long paymentId)
        {
            payment = await _payments.GetByIdAsync(paymentId, ct);
        }

        var detail = new AdminDepositDetailResponse(
            deposit.Id,
            deposit.AuctionId,
            auction?.Product?.Name ?? "Unknown Auction",
            auction?.Status.ToString() ?? "Unknown",
            auction?.WinnerId,
            deposit.UserId,
            user?.Email ?? "Unknown",
            user?.FullName ?? "Unknown",
            user?.BankName,
            user?.BankAccountNumber,
            user?.BankAccountName,
            deposit.Amount,
            deposit.State.ToString(),
            deposit.HeldAt ?? deposit.CreatedAt,
            deposit.PaymentId,
            payment?.Reference,
            deposit.CreatedAt);

        return Result<AdminDepositDetailResponse>.Ok(detail);
    }

    public async Task<Result> ProcessManualRefundAsync(
        long depositId, string reason, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var deposit = await _deposits.GetByIdAsync(depositId, ct);
        if (deposit is null)
        {
            return Result.Fail("Deposit not found.", ApplicationErrorCode.ResourceNotFound);
        }

        if (deposit.State != DepositState.Held)
        {
            return Result.Fail($"Deposit is not in Held state. Current state: {deposit.State}", ApplicationErrorCode.Validation);
        }

        if (deposit.PaymentId is null)
        {
            deposit.LastError = "Cannot refund: deposit has no linked payment.";
            await SaveAsync(ct);
            return Result.Fail("Cannot refund: deposit has no linked payment.", ApplicationErrorCode.Validation);
        }

        var now = DateTimeOffset.UtcNow;
        var refund = new Refund
        {
            PaymentId = deposit.PaymentId.Value,
            Amount = deposit.Amount,
            Reason = reason,
            Status = RefundStatus.Completed,
            ProviderRef = $"REF-DEP-{now:yyyyMMddHHmmss}-{depositId}",
            ProcessedAt = now,
            CreatedAt = now
        };
        _refunds.Add(refund);

        var (transitioned, transitionError) = TryTransition(deposit, DepositState.Refunded);
        if (!transitioned)
        {
            return Result.Fail(
                transitionError ?? "Deposit cannot be refunded.", ApplicationErrorCode.Validation);
        }

        deposit.RefundedAt = DateTimeOffset.UtcNow;
        deposit.Refund = refund;

        var productName = await ResolveProductNameAsync(deposit.AuctionId, ct);
        _notifications.Add(_notificationFactory.DepositRefundInitiated(
            deposit.UserId, deposit.AuctionId, productName, deposit.Amount));

        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return Result.Fail(
                saveError ?? "Deposit could not be saved.",
                ApplicationErrorCode.ConcurrencyConflict);
        }

        await transaction.CommitAsync(ct);
        return Result.Ok();
    }
}
