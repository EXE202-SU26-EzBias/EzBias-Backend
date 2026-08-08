# Payment, Order, Refund, and Payout Flow

## Payment References
- Reference format: `PAY-{yyyyMMddHHmmss}-{userId}`.
- Transfer content format: `EZB-{userId}-{HHmmss}`.
- Order payments and auction deposit payments use the same reference/transfer-content patterns.
- SePay reference extraction normalizes alphanumeric characters, finds `PAY`, expects at least 14 timestamp digits plus a user ID, and reconstructs `PAY-{ts}-{userId}`.

## SePay Integration
- `SePayClient` calls `GET /userapi/transactions/list?account_number={AccountNumber}&limit={DefaultLimit}` against `SePay:BaseUrl`, default `https://my.sepay.vn`.
- Authentication uses `Authorization: Bearer {SePay:ApiToken}`.
- `SePay:DefaultLimit` defaults to `200`.
- HTTP 429 reads `x-sepay-userapi-retry-after` when present and returns a retry hint.
- `POST /api/payments/webhook` is anonymous, accepts the SePay payload shape, rejects non-`in` transfers, extracts the payment reference from content/description, then uses pull-based matching before confirmation.

## Webhook Signature Verification
- Headers: `X-SePay-Signature`, `X-SePay-Timestamp`.
- If `SePay:WebhookSecret` is empty, verification returns true.
- If configured, signature must equal `sha256={HMACSHA256("{timestamp}.{rawBody}", WebhookSecret)}` using fixed-time comparison.
- `HandleSePayWebhookAsync` verifies the raw webhook and then delegates to the older reference-based webhook path, which verifies again with the same raw body.

## Pull-Based Transaction Matching
- Webhooks do not blindly trust the payload amount.
- After reference mapping, `ConfirmBySePayPullAsync` pulls recent SePay transactions.
- A transaction matches when `AmountIn` equals `Payment.Amount` within `0.01` and normalized transaction content contains either `Payment.Reference` or `Payment.TransferContent`.
- On match, `Payment.ProviderTxnId`, `Payment.Payload`, and `Payment.UpdatedAt` are recorded before internal confirmation.

## Fixed-Price Cart Checkout
- Buyer adds active, non-deleted, non-auction products to cart. The buyer cannot add their own product.
- Cart quantity must be positive and cannot exceed current stock.
- `POST /api/orders` requires selected cart item IDs, positive quantities, and an address snapshot with `Address`, `Fullname`, `City`, `Phone`, and `Zip`.
- Checkout groups selected cart items by seller and creates one pending order per seller.
- Product name/image, quantity, unit price, and subtotal are snapshotted onto `OrderItem`.
- Selected cart items are removed after order creation.
- `POST /api/payments` accepts one or more pending order IDs owned by the caller. Any existing payment join for an order blocks creating another payment.
- Confirmation marks the payment paid, marks each order `Paid`, decrements product stock for cart-sourced orders, records escrow/commission, and notifies sellers with `OrderPlaced`.

## Auction Winner Payment
- The scheduler creates a pending auction order when an auction closes with a winner; the initial address snapshot is `{}`.
- The winner can add the won auction item to cart through `POST /api/cart/auction/{auctionId}` while the auction is `EndedPendingPayment`.
- Cart totals and order creation use `Auction.FinalPrice` instead of the product list price for that won auction item.
- If order creation finds the scheduler-created auction order, it updates the order address instead of creating a duplicate.
- Payment creation for a single auction order reduces the amount due by the winner's held deposit. `Order.Total` remains the final bid price.
- Payment confirmation marks the auction order `Paid`, moves the auction `EndedPendingPayment -> Sold`, frees the product from auction mode, and applies the winner deposit `Held -> Applied`.
- Auction orders do not decrement product stock in payment confirmation.

## Auction Deposit Payment
- Deposit initiation creates `Payment.Type = AuctionDeposit`, `Status = Pending`, and amount equal to `Auction.RequiredDepositAmount`.
- Deposit payments have no `PaymentOrder`, no order escrow, and no commission.
- Confirmation first saves `Payment.Status = Paid`, then calls `ConfirmDepositAsync`.
- `ConfirmDepositAsync` transitions the linked deposit `PendingPayment -> Held`, records `HeldAt`, and queues `DepositConfirmed`.

## Manual Confirmation
- `POST /api/payments/{paymentId}/manual-confirm` requires `Admin`.
- It sets a `MANUAL-{adminId}-{yyyyMMddHHmmss}` provider transaction ID if missing, writes a manual JSON payload, and runs the same internal confirmation path used by SePay matching.
- Manual confirmation can confirm order payments or auction deposit payments.

## Escrow and Commission
- Order payment confirmation creates escrow `IN` records once per paid order if no hold exists for the payment.
- `EscrowTransaction.Type = IN` amount is `Order.Total`, linked to the order payment. For auction orders this is the final bid price, even when the bank transfer amount was reduced by a held deposit.
- Commission is recorded only for order payments, not auction deposit payments.
- `ConfiguredCommissionRateProvider` clamps `Commission:RatePercent` to `5..10`; default option value is `8`.
- `CommissionAmount = Math.Round(Order.Total * ratePercent / 100m, 2, MidpointRounding.AwayFromZero)`.
- `SellerNetAmount = Order.Total - CommissionAmount`.
- Duplicate commission records are avoided by checking whether a commission already exists for the payment.

## Seller Fulfillment
- Seller orders are read through `api/seller/orders`.
- Sellers can mark `Paid` or `Processing` orders shipped through `PUT /api/seller/orders/{id}/ship`.
- Shipping stores optional carrier, generates a tracking number as either `tracking - {D6}` or `{carrier} - {D6}`, sets `ShippedAt`, moves status to `Shipped`, and notifies the buyer with `OrderShipped`.
- Buyers can confirm receipt through `PUT /api/orders/{id}/confirm` when order status is `Shipped` or `Delivered`.
- Buyer confirmation sets `DeliveredAt`, moves status to `Delivered`, and notifies the seller with `OrderConfirmed`.

## Delivered Finalization and Payout Creation
- `DeliveredOrderFinalizeScheduler` uses `Order:DeliveredFinalizeScheduler:IntervalSeconds`, default `60`, minimum `1`.
- Grace period uses `Order:DeliveredFinalizeScheduler:GraceDays`, default `3`, minimum `0`.
- Candidate orders must be `Delivered`, have `DeliveredAt <= now - graceDays`, have no open dispute, and have no pending refund.
- Open/pending blockers are absent when no dispute exists or the dispute is `Closed`, `ResolvedBuyer`, or `ResolvedSeller`, and no refund is `Pending`.
- Finalization sets `CompletedAt`, moves status to `Completed`, creates an escrow `OUT` for seller net amount, and creates a pending `Payout` if one does not already exist.
- Seller net amount comes from the `CommissionTransaction`; if missing, finalization falls back to `Order.Total`.

## Disputes and Refunds
- Buyers open disputes through `POST /api/disputes` only for their own `Delivered` orders.
- Disputes are allowed only within 3 days of `DeliveredAt`. The request must include at least one item with a positive requested quantity not exceeding the ordered quantity.
- Opening a dispute moves the order to `ReturnRequested` and notifies all admins with `DisputePendingReview`.
- Only one dispute row can exist per order. If a prior rejected dispute exists, the service reuses that row and replaces its items.
- Admin approve requires an open/under-review dispute, approved items, an unpaid seller payout, and a payment for the order. It creates a `RefundStatus.Pending` refund for the approved amount and marks the dispute `ResolvedBuyer`.
- Admin reject marks the dispute `ResolvedSeller`, stores the reason as admin note, and moves the order back to `Delivered`.
- Admin refund completion marks the latest dispute refund `Completed`, sets `ProviderRef = REF-DSP-{yyyyMMddHHmmss}-{disputeId}`, and notifies the buyer with `DisputeRefundCompleted`.
- Full refunds move the order to `Refunded`; partial refunds move the order to `Completed` and finalize seller payout for the remaining seller flow.
- If completed refunds equal the effective paid amount, the payment status becomes `Refunded`.

## Seller Payout Administration
- Seller payout lists are available at `api/seller/payouts`.
- Admin payout lists and actions are under `api/admin/payouts`.
- Approving a payout is idempotent when already `Approved`; otherwise it sets status `Approved`, `PaidAt`, and `BankTransferRef` from the request or `PO-{yyyyMMddHHmmss}-{payoutId}`, then notifies the seller with `PayoutPaid`.
- Rejecting a payout is idempotent when already `Rejected`; approved payouts cannot be rejected.

## Admin Transactions and Dashboard Metrics
- Admin transaction rows combine payments, payouts, and refunds.
- Admin revenue dashboard uses paid/commission/refund data: gross revenue from paid payments, refunded amount from completed refunds, net revenue as gross minus refunds, and commission revenue from commission transactions.
- Seller dashboard revenue, top listings, and monthly series come from commission transactions, which are the authoritative realized-sales records.
