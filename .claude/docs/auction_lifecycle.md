# Auction Lifecycle

## Seller Auction Management
- Sellers manage auctions through `api/seller/auctions`.
- `CreateAsync` requires an owned product that is not already marked `IsAuction`, a positive floor price, reserve price greater than or equal to floor price when supplied, and `EndsAt > now + 1 minute`.
- Only one live/extended auction may exist for the product at create time.
- Seller-provided `RequiredDepositAmount` is rounded to a whole VND amount and must be between `0` and `FloorPrice`. `0` disables the deposit gate for bidding.
- Creating an auction marks the product `IsAuction = true`, sets `CurrentBid = FloorPrice`, and creates the auction in `Draft`.
- Publish is the intended `Draft -> Live` action. The current guard blocks `Canceled`, `Sold`, `EndedNoWinner`, and `EndedPendingPayment`; keep this guard aligned if stricter state rules are added.
- Cancel is allowed for draft-style auctions and for `Live`/`Extended` auctions with no bids. It sets `Canceled`, frees the product from auction mode, and releases held deposits through the deposit refund flow.
- Relist is allowed only from `Canceled`, `EndedNoWinner`, or `WinnerFailed`. The floor price is copied from the source auction; the seller can set reserve/deposit/timing flags for the new draft. The source stores `RelistedToAuctionId`.

## Status State Machine
- `Draft`: seller-created, not yet public in the default auction list.
- `Draft -> Live`: seller publishes.
- `Live` or `Extended -> Canceled`: seller cancels before any bid exists.
- `Live` or `Extended -> EndedNoWinner`: scheduler closes after end time when no bid exists or the top bid is below reserve.
- `Live` or `Extended -> EndedPendingPayment`: scheduler closes with a qualifying top bid and creates or reuses a pending auction order.
- `EndedPendingPayment -> Sold`: winner order payment is confirmed.
- `EndedPendingPayment -> WinnerFailed`: scheduler finds `WinnerPaymentDeadline <= now`, cancels the pending auction order, frees the product, and forfeits the winner deposit.
- `Canceled`, `EndedNoWinner`, or `WinnerFailed -> Draft`: relist creates a new auction row.

## Public Auction Reads
- `GET /api/auctions` is public. Without a status filter it returns only `Live` and `Extended`; with `status` it returns that status.
- `GET /api/auctions/{auctionId}` is public and returns seller/product details, reserve/current price, extension settings, status, end time, and winner ID.
- `GET /api/auctions/{auctionId}/bids/history` is public and returns bid history with current user profile data from the bid user relation.
- `api/auction-post/buyer/won` lists auctions won by the authenticated buyer; `onlyPendingPayment=true` filters to `EndedPendingPayment`.
- `api/auction-post/seller/ended` lists ended/sold/winner-failed auctions for the authenticated seller.

## Bidding Rules
- Placing a bid requires auth: `POST /api/auctions/{auctionId}/bids`.
- Bids are accepted only when the auction status is `Live` or `Extended` and `EndsAt` is still in the future.
- The seller cannot bid on their own auction.
- If `RequiredDepositAmount > 0`, the bidder must have a `Held` deposit for that auction.
- First bid must be at least the floor price. Later bids must be at least current highest bid plus `1,000` VND.
- The previous winning bid is cleared, the new bid is marked winning, `Auction.CurrentBid` is updated, and the previous winner receives an `Outbid` notification.
- Auction extension logic remains in comments and is currently disabled. Accepted bids force the auction status to `Live`.
- `AuctionBiddingApplicationService` calls `IAuctionRealtime`; the SignalR adapter pushes `BidPlaced` to the group `auction-{auctionId}` after the bid transaction succeeds.

## Deposit Initiation
- `POST /api/auctions/{auctionId}/deposit` requires auth.
- Auction must exist and be `Live` or `Extended`.
- The seller cannot deposit on their own auction.
- If an active `PendingPayment` or `Held` deposit already exists for the caller/auction, the same deposit/payment reference is returned idempotently.
- A new deposit creates a `Payment` with `Type = AuctionDeposit`, `Status = Pending`, `Reference = PAY-{yyyyMMddHHmmss}-{userId}`, `TransferContent = EZB-{userId}-{HHmmss}`, and amount equal to `Auction.RequiredDepositAmount`.
- The deposit is created as `PendingPayment` and linked to the payment.
- Admin users are notified with `DepositPendingReview`.
- `GET /api/auctions/{auctionId}/deposit` returns the caller's latest deposit state and the auction's required deposit amount.

## Deposit State Machine
- Legal transitions are enforced by `DepositApplicationService.TryTransition`.
- `PendingPayment -> Held`: linked payment is confirmed as paid, `HeldAt` is set, amount is copied from the payment, and `DepositConfirmed` is queued.
- `PendingPayment -> Failed`: legal in the transition table, but no public flow currently uses it.
- `Held -> Applied`: winner deposit is consumed toward the final auction order payment.
- `Held -> Forfeited`: winner misses the 24-hour payment deadline.
- `Held -> Refunded`: cancellation release or admin/manual refund processing.
- Terminal states are `Applied`, `Forfeited`, `Refunded`, and `Failed`; transitions out are rejected.
- Concurrency is guarded by PostgreSQL `xmin`; active deposits are unique per user/auction while state is `PendingPayment` or `Held`.

## AuctionCloseScheduler
- Config path: `Auction:CloseScheduler:IntervalSeconds`, default `10`, minimum runtime value `1`.
- Each tick creates a DI scope, then checks near-end auctions, closable live auctions, and expired winner-payment auctions.
- Near-end reminders use the window `now + 4 minutes` to `now + 5 minutes`. Distinct bidders and the seller receive `AuctionEndingSoon`; then `ReminderSent5Min = true`.
- No qualifying winner: status becomes `EndedNoWinner`, `EndedAt` is set, product `IsAuction` becomes false, and the seller receives `AuctionExpired`.
- Qualifying winner: status becomes `EndedPendingPayment`, `WinnerId`, `FinalPrice`, `EndedAt`, and `WinnerPaymentDeadline = now + 24 hours` are set; the winner receives `AuctionWon`.
- If no order exists for the auction, the scheduler creates a pending `Order` with `Source = Auction`, `Total = FinalPrice`, a one-line item at the final bid price, and temporary `AddressSnap = "{}"`.
- Expired winner payment: status becomes `WinnerFailed`, product `IsAuction` becomes false, the pending auction order is canceled, and the winner's held deposit is forfeited.

## Winner Checkout
- The primary winner checkout path is: winner adds the won auction item to cart with `POST /api/cart/auction/{auctionId}`, creates/updates the auction order through `POST /api/orders`, then creates a payment through `POST /api/payments`.
- Cart display and order creation use `Auction.FinalPrice` for a won auction product while the auction is still `EndedPendingPayment`.
- If the scheduler already created the auction order, order creation updates its `AddressSnap` instead of inserting a duplicate order.
- For a single auction order, payment creation computes amount due as `Order.Total - heldDeposit.Amount`, floored at `0`. `Order.Total` remains the final bid price.
- Payment confirmation moves the auction to `Sold`, frees the product from auction mode, and attempts to apply the held winner deposit. A missing/already-moved deposit does not block marking the order paid.
- `POST /api/auctions/{auctionId}/pay` does not create a payment. It only tries to confirm an existing pending auction payment by running the SePay pull-matching path.

## Deposit Refund Policy
- Non-winner auto-refund code is intentionally disabled in `AuctionCloseScheduler`.
- Held losing-bidder deposits remain visible to admins after the auction leaves `Draft`, `Live`, or `Extended`.
- Admins review `GET /api/admin/deposits/pending-refunds`, inspect `GET /api/admin/deposits/{id}`, and process refunds with `POST /api/admin/deposits/{id}/refund`.
- Manual admin deposit refunds create a `Refund` with `Status = Completed`, a `REF-DEP-{yyyyMMddHHmmss}-{depositId}` provider reference, move the deposit `Held -> Refunded`, and notify the bidder.
- Cancellation release uses the shared refund routine and creates `RefundStatus.Pending` refund records while moving deposits to `Refunded`.
- Do not re-enable automatic non-winner refunds without an explicit product decision.
