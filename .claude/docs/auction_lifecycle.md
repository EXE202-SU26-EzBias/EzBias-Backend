# Auction Lifecycle

## Status State Machine
- `Draft`: created by `SellerAuctionApplicationService.CreateAsync`. Product is marked `IsAuction = true`. Seller-provided `RequiredDepositAmount` is rounded to whole VND and must be between 0 and floor price.
- `Draft -> Live`: `SellerAuctionApplicationService.PublishAsync`.
- `Live` or `Extended -> Canceled`: seller cancel if there are no bids. Product is freed with `IsAuction = false`. Held deposits are released by refund flow.
- `Live` or `Extended -> EndedNoWinner`: `AuctionCloseScheduler` when `EndsAt <= now` and no top bid exists or reserve price is not met. Product is freed and seller receives `AuctionExpired`.
- `Live` or `Extended -> EndedPendingPayment`: `AuctionCloseScheduler` when a valid top bid exists and reserve is met. Winner/final price/deadline are set; auction order is created if missing.
- `EndedPendingPayment -> Sold`: order payment confirmation for an auction order in `PaymentApplicationService.ConfirmInternalAsync`.
- `EndedPendingPayment -> WinnerFailed`: `AuctionCloseScheduler` when `WinnerPaymentDeadline <= now`. Product is freed and pending auction order is canceled.
- `Canceled`, `EndedNoWinner`, or `WinnerFailed -> Draft`: relist creates a new auction; the source auction records `RelistedToAuctionId`.

## Bidding Rules
- Public auction reads are anonymous; placing a bid requires `[Authorize]`.
- Bids are allowed only when status is `Live` or `Extended` and `EndsAt` is in the future.
- Seller cannot bid on own auction.
- If `RequiredDepositAmount > 0`, bidder must have a `Held` deposit for the auction.
- First bid must be at least `FloorPrice`; later bids must exceed current highest by at least `1,000` VND.
- Previous winning bid is cleared, new bid is marked `IsWinning = true`, `Auction.CurrentBid` is updated, and `Outbid` notification is queued.
- Extension logic exists in comments but is currently disabled; accepted bids force status back to `Live`.

## Deposit State Machine
- Legal transitions are enforced in `DepositApplicationService.TryTransition`.
- `PendingPayment -> Held`: payment is marked paid and `ConfirmDepositAsync` confirms the linked auction deposit.
- `PendingPayment -> Failed`: legal in the transition table, but no current public flow uses it.
- `Held -> Applied`: winner deposit is consumed toward the final auction order payment.
- `Held -> Forfeited`: winner misses the 24-hour payment deadline.
- `Held -> Refunded`: auction cancellation or manual/admin refund processing.
- Terminal states are `Applied`, `Forfeited`, `Refunded`, and `Failed`; transitions out are rejected.
- `AuctionDeposit` uses PostgreSQL `xmin` optimistic concurrency, and active deposits are unique per user/auction while `PendingPayment` or `Held`.

## Deposit Initiation
- `POST /api/auctions/{auctionId}/deposit` requires auth.
- Auction must exist and be `Live` or `Extended`.
- Seller cannot deposit on own auction.
- Existing `PendingPayment` or `Held` deposit is returned idempotently.
- New deposit creates a `Payment` with type `AuctionDeposit`, status `Pending`, `Reference = PAY-{yyyyMMddHHmmss}-{userId}`, and `TransferContent = EZB-{userId}-{HHmmss}`.
- Admins are notified with `DepositPendingReview`.

## AuctionCloseScheduler
- Config path: `Auction:CloseScheduler:IntervalSeconds`, default `10`, minimum `1`.
- Each tick creates a DI scope, reads closable auctions, near-end auctions, and expired winner-payment auctions.
- Near-end reminder query uses `now + 4 minutes` to `now + 5 minutes`; each distinct bidder and the seller receive `AuctionEndingSoon`, then `ReminderSent5Min = true`.
- Closing with no qualifying winner sets `EndedNoWinner`, frees product, notifies seller, and records the auction for deposit handling.
- Closing with a qualifying winner sets `EndedPendingPayment`, `WinnerId`, `FinalPrice`, `WinnerPaymentDeadline = now + 24 hours`, sends `AuctionWon`, and creates a pending auction order if one does not exist.
- Expired winner payment sets `WinnerFailed`, frees product, cancels pending auction order, and forfeits the winner's held deposit.

## Winner Payment Deadline
- Winner has 24 hours from scheduler close time.
- `WinnerPaymentDeadline` is stored on `Auction`.
- `GetPendingPaymentExpiredAsync(now)` drives `WinnerFailed`.
- For auction order payment, the held deposit reduces the amount due via `ComputeWinnerAmountDueAsync`; `Order.Total` remains final price.

## WinnerFailed Flow
- Scheduler sets auction `WinnerFailed`.
- Product is released from auction mode.
- Pending auction order is canceled.
- `ForfeitWinnerDepositAsync` transitions winner deposit `Held -> Forfeited`, records `ForfeitedAt`, sends `DepositForfeited`, and retries save up to 3 times.

## Non-Winner Refund Policy
- Non-winner deposit auto-refund code is intentionally commented out in `AuctionCloseScheduler`.
- Held losing-bidder deposits remain available for admin review.
- Admins use deposit management APIs backed by `GetPendingRefundsAsync`, `GetDepositDetailAsync`, and `ProcessManualRefundAsync`.
- Do not re-enable automatic non-winner refunds without an explicit product decision.
