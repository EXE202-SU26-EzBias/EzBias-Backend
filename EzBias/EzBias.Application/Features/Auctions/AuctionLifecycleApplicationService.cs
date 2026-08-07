using EzBias.Application.Features.Deposits;
using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Auctions;

public sealed class AuctionLifecycleApplicationService : IAuctionLifecycleApplicationService
{
    private readonly IAuctionRepository _auctions;
    private readonly IBidRepository _bids;
    private readonly IOrderRepository _orders;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IDepositApplicationService _deposits;
    private readonly IUnitOfWork _uow;

    public AuctionLifecycleApplicationService(
        IAuctionRepository auctions,
        IBidRepository bids,
        IOrderRepository orders,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IDepositApplicationService deposits,
        IUnitOfWork uow)
    {
        _auctions = auctions;
        _bids = bids;
        _orders = orders;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _deposits = deposits;
        _uow = uow;
    }

    public async Task<AuctionLifecycleRunResult> RunAsync(
        DateTimeOffset now,
        CancellationToken ct)
    {
        var errors = new List<string>();

        var reminderIds = (await _auctions.GetNearEndAsync(
                now.AddMinutes(4),
                now.AddMinutes(5),
                ct))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        _uow.ClearTrackedChanges();

        var remindersSent = 0;
        foreach (var auctionId in reminderIds)
        {
            try
            {
                if (await ProcessReminderAsync(auctionId, now, ct))
                    remindersSent++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"Reminder auction {auctionId}: {ex.Message}");
            }
            finally
            {
                _uow.ClearTrackedChanges();
            }
        }

        var closableIds = (await _auctions.GetClosableAsync(now, ct))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        _uow.ClearTrackedChanges();

        var endedNoWinner = 0;
        var pendingPayment = 0;
        foreach (var auctionId in closableIds)
        {
            try
            {
                var outcome = await CloseAuctionAsync(auctionId, now, ct);
                if (outcome == AuctionCloseOutcome.NoWinner)
                    endedNoWinner++;
                else if (outcome == AuctionCloseOutcome.PendingPayment)
                    pendingPayment++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"Close auction {auctionId}: {ex.Message}");
            }
            finally
            {
                _uow.ClearTrackedChanges();
            }
        }

        var expiredIds = (await _auctions.GetPendingPaymentExpiredAsync(now, ct))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        _uow.ClearTrackedChanges();

        var winnerFailed = 0;
        foreach (var auctionId in expiredIds)
        {
            try
            {
                if (await MarkWinnerFailedAsync(auctionId, now, ct))
                    winnerFailed++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"Winner timeout auction {auctionId}: {ex.Message}");
            }
            finally
            {
                _uow.ClearTrackedChanges();
            }
        }

        return new AuctionLifecycleRunResult(
            remindersSent,
            endedNoWinner,
            pendingPayment,
            winnerFailed,
            errors);
    }

    private async Task<bool> ProcessReminderAsync(
        long auctionId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        var auction = await _auctions.GetByIdWithProductForUpdateAsync(auctionId, ct);
        if (auction is null
            || auction.ReminderSent5Min
            || auction.Status is not (AuctionStatus.Live or AuctionStatus.Extended)
            || auction.EndsAt < now.AddMinutes(4)
            || auction.EndsAt > now.AddMinutes(5))
            return false;

        var bidderIds = (await _bids.GetByAuctionIdAsync(auction.Id, ct))
            .Select(x => x.UserId)
            .Distinct();
        foreach (var bidderId in bidderIds)
            _notifications.Add(_notificationFactory.AuctionEndingSoon(
                bidderId,
                auction.Id,
                auction.Product.Name,
                5));

        _notifications.Add(_notificationFactory.AuctionEndingSoon(
            auction.SellerId,
            auction.Id,
            auction.Product.Name,
            5));
        auction.ReminderSent5Min = true;
        auction.UpdatedAt = now;

        await _uow.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private async Task<AuctionCloseOutcome> CloseAuctionAsync(
        long auctionId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        var auction = await _auctions.GetByIdWithProductForUpdateAsync(auctionId, ct);
        if (auction is null
            || auction.Status is not (AuctionStatus.Live or AuctionStatus.Extended)
            || auction.EndsAt > now)
            return AuctionCloseOutcome.Skipped;

        var topBid = await _bids.GetTopBidAsync(auction.Id, ct);
        if (topBid is null
            || (auction.ReservePrice.HasValue && topBid.Amount < auction.ReservePrice.Value))
        {
            if (auction.MarkEndedNoWinner(now) == TransitionOutcome.Invalid)
                return AuctionCloseOutcome.Skipped;

            auction.Product.IsAuction = false;
            auction.Product.UpdatedAt = now;
            _notifications.Add(_notificationFactory.AuctionExpired(
                auction.SellerId,
                auction.Id,
                auction.Product.Name));

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return AuctionCloseOutcome.NoWinner;
        }

        if (auction.AssignWinner(
                topBid.UserId,
                topBid.Amount,
                now.AddHours(24),
                now) == TransitionOutcome.Invalid)
            return AuctionCloseOutcome.Skipped;

        _notifications.Add(_notificationFactory.AuctionWon(
            topBid.UserId,
            auction.Id,
            auction.Product.Name,
            topBid.Amount));

        var order = await _orders.GetByAuctionIdAsync(auction.Id, ct);
        if (order is null)
        {
            var productImage = auction.Product.Images
                .OrderBy(x => x.SortOrder)
                .FirstOrDefault()?.Url
                ?? auction.Product.PrimaryImageUrl
                ?? string.Empty;

            order = new Order
            {
                UserId = topBid.UserId,
                SellerId = auction.SellerId,
                Source = OrderSource.Auction,
                AuctionId = auction.Id,
                Total = topBid.Amount,
                Status = OrderStatus.Pending,
                AddressSnap = "{}",
                CreatedAt = now,
                Items =
                {
                    new OrderItem
                    {
                        ProductId = auction.ProductId,
                        ProductName = auction.Product.Name,
                        ProductImage = productImage,
                        Quantity = 1,
                        UnitPrice = topBid.Amount,
                        Subtotal = topBid.Amount
                    }
                }
            };
            _orders.Add(order);
        }

        await _uow.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return AuctionCloseOutcome.PendingPayment;
    }

    private async Task<bool> MarkWinnerFailedAsync(
        long auctionId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        var auction = await _auctions.GetByIdWithProductForUpdateAsync(auctionId, ct);
        if (auction is null
            || auction.Status != AuctionStatus.EndedPendingPayment
            || !auction.WinnerPaymentDeadline.HasValue
            || auction.WinnerPaymentDeadline.Value > now
            || auction.MarkWinnerFailed(now) == TransitionOutcome.Invalid)
            return false;

        auction.Product.IsAuction = false;
        auction.Product.UpdatedAt = now;

        var order = await _orders.GetByAuctionIdAsync(auction.Id, ct);
        if (order is not null && order.Status == OrderStatus.Pending)
            order.MarkCanceled(now);

        if (auction.WinnerId.HasValue)
        {
            var forfeit = await _deposits.ForfeitWinnerDepositAsync(
                auction.Id,
                auction.WinnerId.Value,
                ct);
            if (!forfeit.IsSuccess)
                throw new InvalidOperationException(
                    forfeit.Failure?.Message ?? "Winner deposit could not be forfeited.");
        }

        await _uow.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private enum AuctionCloseOutcome
    {
        Skipped,
        NoWinner,
        PendingPayment
    }
}
