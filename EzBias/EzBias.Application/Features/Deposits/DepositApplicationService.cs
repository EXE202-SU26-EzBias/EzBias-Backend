using EzBias.Application.Features.Deposits.Dtos;
using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Application.Features.Deposits;

/// <summary>
/// Deposit_Service: creates, confirms, applies, forfeits, and refunds <see cref="AuctionDeposit"/>
/// records. All state changes flow through the private transition guard
/// (<see cref="TryTransition(AuctionDeposit, DepositState, out TransitionOutcome)"/>) which enforces
/// the legal <see cref="DepositState"/> transition table and rejects transitions out of terminal
/// states (Req 10.1, 10.2). Concurrency (Req 10.6) is enforced by the optimistic concurrency token
/// on the persisted row (see the note in <see cref="ApplyTransitionAndSaveAsync"/>).
/// </summary>
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
    private readonly IDepositPolicy _depositPolicy;
    private readonly IUnitOfWork _uow;

    public DepositApplicationService(
        IAuctionDepositRepository deposits,
        IAuctionRepository auctions,
        IPaymentRepository payments,
        IRefundRepository refunds,
        IUserRepository users,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IDepositPolicy depositPolicy,
        IUnitOfWork uow)
    {
        _deposits = deposits;
        _auctions = auctions;
        _payments = payments;
        _refunds = refunds;
        _users = users;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _depositPolicy = depositPolicy;
        _uow = uow;
    }

    // ---------------------------------------------------------------------
    // Transition guard (Req 10.1, 10.2, 10.4)
    // ---------------------------------------------------------------------

    /// <summary>Classifies the outcome of a requested deposit-state transition.</summary>
    private enum TransitionOutcome
    {
        Applied,   // transition was legal and has been applied to the deposit
        Terminal,  // the deposit is in a terminal state; transition rejected
        Illegal    // the transition is not part of the legal transition table; rejected
    }

    /// <summary>
    /// Terminal states from which no further transition is permitted (Req 10.2).
    /// <c>Failed</c> is also treated as terminal.
    /// </summary>
    private static bool IsTerminal(DepositState state) =>
        state is DepositState.Applied
              or DepositState.Forfeited
              or DepositState.Refunded
              or DepositState.Failed;

    /// <summary>
    /// The legal transition table:
    /// <c>PendingPayment -> {Held, Failed}</c>; <c>Held -> {Refunded, Applied, Forfeited}</c>.
    /// Every other (from, to) pair is illegal.
    /// </summary>
    private static bool IsLegalTransition(DepositState from, DepositState to) => from switch
    {
        DepositState.PendingPayment => to is DepositState.Held or DepositState.Failed,
        DepositState.Held => to is DepositState.Refunded or DepositState.Applied or DepositState.Forfeited,
        _ => false
    };

    /// <summary>
    /// Applies <paramref name="to"/> to <paramref name="deposit"/> only when the transition is legal
    /// and the deposit is not already terminal. On success the deposit's <see cref="AuctionDeposit.State"/>
    /// and <see cref="AuctionDeposit.UpdatedAt"/> are updated; on rejection the deposit's state and
    /// recorded amount are left unchanged (Req 10.1, 10.2). Convenience overload that discards the
    /// detailed outcome classification.
    /// </summary>
    private static (bool Success, string? Error) TryTransition(AuctionDeposit deposit, DepositState to)
        => TryTransition(deposit, to, out _);

    /// <summary>
    /// Applies <paramref name="to"/> to <paramref name="deposit"/> only when legal. The
    /// <paramref name="outcome"/> out-parameter lets callers distinguish a terminal-state rejection
    /// from an illegal-transition rejection (e.g. to treat an idempotent re-process as a no-op).
    /// </summary>
    private static (bool Success, string? Error) TryTransition(
        AuctionDeposit deposit, DepositState to, out TransitionOutcome outcome)
    {
        var from = deposit.State;

        // Reject any transition requested FROM a terminal state, leaving state/amount unchanged.
        if (IsTerminal(from))
        {
            outcome = TransitionOutcome.Terminal;
            return (false, TerminalStateError);
        }

        if (!IsLegalTransition(from, to))
        {
            outcome = TransitionOutcome.Illegal;
            return (false, IllegalTransitionError);
        }

        deposit.State = to;
        deposit.UpdatedAt = DateTimeOffset.UtcNow;
        outcome = TransitionOutcome.Applied;
        return (true, null);
    }

    /// <summary>
    /// Shared helper used by the lifecycle methods: applies a legal transition via
    /// <see cref="TryTransition(AuctionDeposit, DepositState, out TransitionOutcome)"/> and, when the
    /// transition is applied, persists it through the unit of work (mapping concurrency conflicts).
    /// </summary>
    /// <remarks>
    /// Concurrency (Req 10.6): optimistic concurrency via the <c>xmin</c> token is enforced at the
    /// DbContext level; <see cref="IUnitOfWork.SaveChangesAsync"/> throws
    /// <see cref="DbUpdateConcurrencyException"/> on conflict, which <see cref="SaveAsync"/> maps to a
    /// concurrency-rejected error so that two racing transitions result in at most one committed change.
    /// </remarks>
    private async Task<(bool Success, string? Error, TransitionOutcome Outcome)> ApplyTransitionAndSaveAsync(
        AuctionDeposit deposit, DepositState to, CancellationToken ct)
    {
        var (success, error) = TryTransition(deposit, to, out var outcome);
        if (!success)
        {
            return (false, error, outcome);
        }

        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return (false, saveError, outcome);
        }

        return (true, null, outcome);
    }

    /// <summary>
    /// Centralizes the persistence + concurrency mapping used by the lifecycle methods. Commits the
    /// current unit of work and maps an optimistic-concurrency conflict (Req 10.6) to a stable,
    /// caller-friendly error so that, when two transitions race on the same deposit, the losing call
    /// is rejected and the row is left in exactly one committed state.
    /// </summary>
    private async Task<(bool Ok, string? Error)> SaveAsync(CancellationToken ct)
    {
        try
        {
            await _uow.SaveChangesAsync(ct);
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, ConcurrencyError);
        }
    }

    // ---------------------------------------------------------------------
    // IDepositApplicationService — method bodies filled in by tasks
    // 5.6, 5.12, 5.16, 5.20, 5.25, 5.29. Stubbed here so the class compiles
    // and is DI-ready.
    // ---------------------------------------------------------------------

    // Req 2 — implemented in task 5.6
    public async Task<(bool Success, string? Error, InitiateDepositResponse? Data)> InitiateDepositAsync(
        long userId, long auctionId, CancellationToken ct)
    {
        // (1) Auction must exist (supports controller 404).
        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null)
        {
            return (false, "Auction not found.", null);
        }

        // (2) Auction must be open for deposits — Live or Extended (Req 2.3).
        if (auction.Status is not AuctionStatus.Live and not AuctionStatus.Extended)
        {
            return (false, "Auction is not open for deposits.", null);
        }

        // (3) A seller cannot deposit on an auction they own (Req 2.4).
        if (auction.SellerId == userId)
        {
            return (false, "A seller cannot deposit on an owned auction.", null);
        }

        // (4) Single-active / idempotency: return any existing PendingPayment/Held deposit
        //     without creating a new deposit or payment (Req 2.5, 2.6, 10.3, 10.5).
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
                existing.State.ToString(),
                existingPayment?.Reference ?? string.Empty,
                existingPayment?.TransferContent ?? string.Empty,
                existing.Amount,
                existingPayment?.Currency ?? "VND");

            return (true, null, existingResponse);
        }

        // (5) Otherwise create a new PendingPayment deposit plus its linked SePay payment.
        var now = DateTimeOffset.UtcNow;

        // Reuse the existing PAY-{14digits}-{userId} reference format so the SePay confirmation
        // parser (ExtractReference) works unchanged. The filtered unique index already bounds
        // concurrent active deposits per user/auction, so the format must NOT be changed.
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
            // Link via navigation so EF assigns deposit.PaymentId once both rows are saved.
            Payment = payment
        };
        _deposits.Add(deposit);

        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return (false, saveError, null);
        }

        var response = new InitiateDepositResponse(
            deposit.Id,
            auctionId,
            deposit.State.ToString(),
            payment.Reference,
            payment.TransferContent!,
            payment.Amount,
            payment.Currency);

        return (true, null, response);
    }

    // Req 9 — implemented in task 5.29
    // Returns the caller's OWN latest deposit for the auction (amount, state, linked payment
    // reference) plus the auction's RequiredDepositAmount. Never exposes another user's deposit.
    public async Task<(bool Success, string? Error, DepositStatusResponse? Data)> GetMyDepositStatusAsync(
        long userId, long auctionId, CancellationToken ct)
    {
        // (1) Auction must exist (Req 9.4 — controller maps to 404).
        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null)
        {
            return (false, "Auction not found.", null);
        }

        // (2) Load only THIS user's latest deposit (Req 9.3 — never exposes others').
        var deposit = await _deposits.GetLatestByUserAndAuctionAsync(userId, auctionId, ct);

        // (3) No deposit yet (Req 9.2): report HasDeposit = false alongside the required amount.
        if (deposit is null)
        {
            return (true, null, new DepositStatusResponse(
                auctionId,
                auction.RequiredDepositAmount,
                HasDeposit: false,
                DepositId: null,
                Amount: null,
                State: null,
                PaymentReference: null));
        }

        // (4) Resolve the linked payment reference when a payment is linked (Req 9.1).
        string? reference = null;
        if (deposit.PaymentId is long pid)
        {
            var p = await _payments.GetByIdAsync(pid, ct);
            reference = p?.Reference;
        }

        return (true, null, new DepositStatusResponse(
            auctionId,
            auction.RequiredDepositAmount,
            HasDeposit: true,
            DepositId: deposit.Id,
            Amount: deposit.Amount,
            State: deposit.State.ToString(),
            PaymentReference: reference));
    }

    // Req 3 — implemented in task 5.12
    // Called from PaymentApplicationService.ConfirmInternalAsync AFTER an AuctionDeposit payment is
    // set to Paid. Transitions the linked deposit PendingPayment -> Held, records the held amount and
    // timestamp, and queues a single confirmation notification.
    public async Task<(bool Success, string? Error)> ConfirmDepositAsync(long paymentId, CancellationToken ct)
    {
        // (1) Locate the deposit by its linked payment.
        var deposit = await _deposits.GetByPaymentIdAsync(paymentId, ct);
        if (deposit is null)
        {
            return (false, "Deposit not found for payment.");
        }

        // (2) Idempotency (Req 3.5): a confirmation arriving for an already-Held deposit is a no-op —
        //     no state change, no extra notification.
        if (deposit.State == DepositState.Held)
        {
            return (true, null);
        }

        // (3) Verification / legal-state guard (Req 3.6): only a PendingPayment deposit may be held.
        //     For any other (non-terminal/terminal) state, leave it unchanged, record the failure, and
        //     reject — without attempting a transition.
        if (deposit.State != DepositState.PendingPayment)
        {
            deposit.LastError = "Deposit not in PendingPayment state at confirmation.";
            var (savedFail, saveFailError) = await SaveAsync(ct);
            if (!savedFail)
            {
                return (false, saveFailError);
            }

            return (false, "Deposit is not awaiting payment.");
        }

        // (4) Read the confirmed payment amount; the held amount must equal it (Req 3.2). Fall back to
        //     the deposit's recorded amount if the payment row cannot be loaded.
        var payment = await _payments.GetByIdAsync(paymentId, ct);

        // (5) Transition PendingPayment -> Held via the guard, recording held amount and UTC timestamp.
        deposit.Amount = payment?.Amount ?? deposit.Amount;
        deposit.HeldAt = DateTimeOffset.UtcNow;

        var (transitioned, transitionError) = TryTransition(deposit, DepositState.Held);
        if (!transitioned)
        {
            return (false, transitionError);
        }

        // (6) Confirmation notification (Req 3.3). Load the auction WITH its Product so the message can
        //     name the item; GetByIdAsync does not include Product, so use GetByIdWithProductAsync.
        var productName = await ResolveProductNameAsync(deposit.AuctionId, ct);
        _notifications.Add(_notificationFactory.DepositConfirmed(
            deposit.UserId, deposit.AuctionId, productName, deposit.Amount));

        // Req 3.4: the realtime push is best-effort and dispatched by NotificationDispatchingUnitOfWork
        // AFTER save; delivery failures there are swallowed and never roll back the Held state. We cannot
        // observe delivery here, so this flag stays false (it reflects confirmed realtime delivery only).
        deposit.ConfirmationNotificationDelivered = false;

        // (7) Persist. A concurrency conflict (Req 10.6) is surfaced as an error; the deposit row stays
        //     in exactly one committed state.
        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return (false, saveError);
        }

        return (true, null);
    }

    /// <summary>
    /// Resolves a human-readable product name for an auction's notifications. Loads the auction WITH its
    /// <see cref="Auction.Product"/> navigation (plain <c>GetByIdAsync</c> does not include it) and falls
    /// back to a generic label when the auction or product is unavailable.
    /// </summary>
    private async Task<string> ResolveProductNameAsync(long auctionId, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdWithProductAsync(auctionId, ct);
        return auction?.Product?.Name ?? "the auction item";
    }

    // Req 5 — implemented in task 5.20
    // Closing an auction that assigned a winner: refund every NON-winner Held deposit and keep the
    // winner's deposit Held (Req 5.1, 6.1). When winnerId is null the auction ended with no winner, so
    // every Held deposit is refunded (Req 5.4).
    public async Task<(bool Success, string? Error)> RefundNonWinnerDepositsAsync(
        long auctionId, long? winnerId, CancellationToken ct)
        => await RefundHeldDepositsAsync(auctionId, winnerId, ct);

    /// <summary>
    /// Shared refund routine for close-time non-winner refunds (Req 5) and cancellation releases
    /// (Req 8). Transitions every Held deposit for <paramref name="auctionId"/> — excluding
    /// <paramref name="excludeUserId"/> when supplied — to <see cref="DepositState.Refunded"/>,
    /// creating exactly one <see cref="Refund"/> per deposit for the full held amount (including zero),
    /// linking it so <c>RefundId</c> is set on save, and queuing one refund notification per deposit.
    /// </summary>
    /// <remarks>
    /// Idempotent (Req 5.6): <see cref="IAuctionDepositRepository.GetHeldByAuctionAsync"/> only returns
    /// deposits whose <see cref="AuctionDeposit.State"/> is <see cref="DepositState.Held"/>, so an
    /// already-Refunded deposit is never reprocessed and no duplicate <see cref="Refund"/> is created.
    /// Failure-isolating (Req 5.5, 8.4): per-deposit work is wrapped in try/catch so a failure on one
    /// deposit leaves it Held and processing continues with the others; the failure is never rethrown.
    /// Notification delivery is best-effort post-save and never rolls back a Refunded deposit
    /// (Req 5.7, 8.5).
    /// </remarks>
    private async Task<(bool Success, string? Error)> RefundHeldDepositsAsync(
        long auctionId, long? excludeUserId, CancellationToken ct)
    {
        // (1) Only Held deposits are candidates (the winner is excluded when excludeUserId is set).
        var held = await _deposits.GetHeldByAuctionAsync(auctionId, excludeUserId, ct);

        // (2) Nothing Held → nothing to do. Idempotent: already-Refunded deposits are never returned.
        if (held.Count == 0)
        {
            return (true, null);
        }

        // (3) Resolve the product name once for all refund notifications on this auction.
        var productName = await ResolveProductNameAsync(auctionId, ct);

        foreach (var deposit in held)
        {
            try
            {
                // A deposit with no linked payment cannot be refunded; leave it Held and record why.
                if (deposit.PaymentId is null)
                {
                    deposit.LastError = "Cannot refund: deposit has no linked payment.";
                    continue;
                }

                // (4a) One Refund per deposit for the full held amount (including zero) — Req 5.2/8.2.
                var refund = new Refund
                {
                    PaymentId = deposit.PaymentId.Value,
                    Amount = deposit.Amount,
                    Reason = "Auction deposit refund.",
                    Status = RefundStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _refunds.Add(refund);

                // (4b) Held → Refunded via the guard; on rejection leave it Held and record the error.
                var (ok, err) = TryTransition(deposit, DepositState.Refunded);
                if (!ok)
                {
                    deposit.LastError = err;
                    continue;
                }

                deposit.RefundedAt = DateTimeOffset.UtcNow;
                // Link the Refund navigation so EF assigns deposit.RefundId once both rows are saved.
                deposit.Refund = refund;

                // (4c) One refund-initiated notification per refunded deposit — Req 5.3/8.3.
                _notifications.Add(_notificationFactory.DepositRefundInitiated(
                    deposit.UserId, auctionId, productName, deposit.Amount));
            }
            catch (Exception ex)
            {
                // (4d) Failure isolation (Req 5.5/8.4): leave this deposit Held and continue with the rest.
                deposit.LastError = $"Refund failed: {ex.Message}";
            }
        }

        // (5) Persist all refunds/transitions together. Notification delivery is best-effort after save
        //     and must not roll back the Refunded state (Req 5.7/8.5).
        return await SaveAsync(ct);
    }

    // Req 6.3/6.5 — implemented in task 5.16
    // Called either when the winner's remaining-balance payment is confirmed, or when a zero-due order
    // is finalized. Transitions the winner's Held deposit -> Applied and records AppliedAt.
    public async Task<(bool Success, string? Error)> ApplyWinnerDepositAsync(
        long auctionId, long winnerId, CancellationToken ct)
    {
        // (1) Load the winner's Held deposit for this auction.
        var deposit = await _deposits.GetHeldByUserAndAuctionAsync(winnerId, auctionId, ct);

        // (2) Req 6.6: with no Held deposit there is nothing to apply.
        if (deposit is null)
        {
            return (false, "No held deposit available to apply.");
        }

        // (3) Transition Held -> Applied via the guard, recording the application timestamp.
        deposit.AppliedAt = DateTimeOffset.UtcNow;
        var (ok, err) = TryTransition(deposit, DepositState.Applied);
        if (!ok)
        {
            return (false, err);
        }

        // (4) Persist; a concurrency conflict (Req 10.6) is surfaced as an error.
        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return (false, saveError);
        }

        return (true, null);
    }

    // Req 6.2/6.4/6.6 — implemented in task 5.16
    // Pure computation/query: never mutates deposit state or saves. Returns the remaining balance the
    // winner must pay after crediting their held deposit toward the final price.
    public async Task<(bool Success, string? Error, decimal AmountDue)> ComputeWinnerAmountDueAsync(
        long auctionId, long winnerId, decimal finalPrice, CancellationToken ct)
    {
        // (1) Load the winner's Held deposit for this auction.
        var deposit = await _deposits.GetHeldByUserAndAuctionAsync(winnerId, auctionId, ct);

        // (2) Req 6.6: no Held deposit to apply — full final price is due, no transition.
        if (deposit is null)
        {
            return (false, "No held deposit available to apply.", finalPrice);
        }

        var held = deposit.Amount;

        // (3) Req 6.4: a deposit covering the whole price leaves nothing due.
        if (held >= finalPrice)
        {
            return (true, null, 0m);
        }

        // (4) Req 6.2: otherwise the remaining balance is the price minus the held amount. Because both
        //     are whole VND and held < finalPrice, the result is naturally >= 1 VND (>= 0.01 VND).
        var amountDue = finalPrice - held;
        return (true, null, amountDue);
    }

    // Req 7 — implemented in task 5.25
    // Called when an auction is marked WinnerFailed. Transitions the winner's Held deposit -> Forfeited,
    // records ForfeitedAt and the auction reference, queues one DepositForfeited notification, and
    // retries the persist up to 3 times tracking ForfeitRetryCount. A non-Held winner deposit is left
    // unchanged and treated as a non-error (Req 7.5) so the scheduler does not fail.
    public async Task<(bool Success, string? Error)> ForfeitWinnerDepositAsync(
        long auctionId, long winnerId, CancellationToken ct)
    {
        // (1) Load the winner's Held deposit for this auction.
        var deposit = await _deposits.GetHeldByUserAndAuctionAsync(winnerId, auctionId, ct);

        // (2) Req 7.5: no Held deposit (e.g. already Applied/Refunded) — nothing to forfeit, not an
        //     error. Leave any non-Held deposit unchanged.
        if (deposit is null)
        {
            return (true, null);
        }

        // (3) Resolve the product name for the forfeiture notification.
        var productName = await ResolveProductNameAsync(auctionId, ct);

        // (4) Record the forfeiture timestamp; the auction reference is deposit.AuctionId (Req 7.3).
        deposit.ForfeitedAt = DateTimeOffset.UtcNow;

        // (5) Transition Held -> Forfeited via the guard. A guard rejection (e.g. the deposit was
        //     concurrently moved away from Held) is a state issue, not a transient save failure, so do
        //     not retry — record the error and reject (Req 7.6).
        var (ok, err) = TryTransition(deposit, DepositState.Forfeited);
        if (!ok)
        {
            deposit.LastError = err;
            return (false, err);
        }

        // (6) One forfeiture notification to the winner (Req 7.2).
        _notifications.Add(_notificationFactory.DepositForfeited(
            deposit.UserId, auctionId, productName, deposit.Amount));

        // (7) Persist with retry up to 3 attempts on a transient/concurrency save failure (Req 7.6).
        //     SaveAsync commits the whole unit of work, so re-calling it after a transient failure is
        //     safe — the staged state change and notification persist together on success.
        string? lastSaveError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var (saved, saveError) = await SaveAsync(ct);
            if (saved)
            {
                return (true, null);
            }

            deposit.ForfeitRetryCount = attempt;
            lastSaveError = saveError;
        }

        // (8) Exhausted all attempts: record the failure. Best-effort persist of the error/retry count.
        deposit.LastError = "Forfeiture did not complete after 3 attempts.";
        await SaveAsync(ct);
        return (false, lastSaveError ?? "Forfeiture did not complete after 3 attempts.");
    }

    // Req 8 — implemented in task 5.20
    // Auction canceled: refund every Held deposit for the auction (no exclusions) — Req 8.1.
    public async Task<(bool Success, string? Error)> ReleaseDepositsOnCancelAsync(long auctionId, CancellationToken ct)
        => await RefundHeldDepositsAsync(auctionId, null, ct);

    // -------------------------------------------------------------------------
    // Admin deposit management methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all Held deposits across all auctions for admin review. These are deposits that may need
    /// manual refund processing when bidders lose auctions.
    /// </summary>
    public async Task<(bool Success, string? Error, IReadOnlyList<AdminDepositListItem>? Data)> GetPendingRefundsAsync(
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

        return (true, null, items);
    }

    /// <summary>
    /// Returns detailed information about a specific deposit for admin review.
    /// </summary>
    public async Task<(bool Success, string? Error, AdminDepositDetailResponse? Data)> GetDepositDetailAsync(
        long depositId, CancellationToken ct)
    {
        var deposit = await _deposits.GetByIdAsync(depositId, ct);
        if (deposit is null)
        {
            return (false, "Deposit not found.", null);
        }

        // Load related entities
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

        return (true, null, detail);
    }

    /// <summary>
    /// Admin manually processes a refund for a Held deposit. This is typically used for losing bidders
    /// whose deposits need to be refunded after an auction closes. Transitions the deposit from Held to
    /// Refunded, creates a Refund record, and sends a notification to the user.
    /// </summary>
    public async Task<(bool Success, string? Error)> ProcessManualRefundAsync(
        long depositId, string reason, CancellationToken ct)
    {
        // (1) Load the deposit by ID
        var deposit = await _deposits.GetByIdAsync(depositId, ct);
        if (deposit is null)
        {
            return (false, "Deposit not found.");
        }

        // (2) Verify deposit is in Held state
        if (deposit.State != DepositState.Held)
        {
            return (false, $"Deposit is not in Held state. Current state: {deposit.State}");
        }

        // (3) Verify deposit has a linked payment
        if (deposit.PaymentId is null)
        {
            deposit.LastError = "Cannot refund: deposit has no linked payment.";
            await SaveAsync(ct);
            return (false, "Cannot refund: deposit has no linked payment.");
        }

        // (4) Create a Refund record
        var refund = new Refund
        {
            PaymentId = deposit.PaymentId.Value,
            Amount = deposit.Amount,
            Reason = reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _refunds.Add(refund);

        // (5) Transition Held -> Refunded
        var (transitioned, transitionError) = TryTransition(deposit, DepositState.Refunded);
        if (!transitioned)
        {
            return (false, transitionError);
        }

        deposit.RefundedAt = DateTimeOffset.UtcNow;
        deposit.Refund = refund; // Link so EF assigns deposit.RefundId on save

        // (6) Send notification to the user
        var productName = await ResolveProductNameAsync(deposit.AuctionId, ct);
        _notifications.Add(_notificationFactory.DepositRefundInitiated(
            deposit.UserId, deposit.AuctionId, productName, deposit.Amount));

        // (7) Persist all changes
        var (saved, saveError) = await SaveAsync(ct);
        if (!saved)
        {
            return (false, saveError);
        }

        return (true, null);
    }
}
