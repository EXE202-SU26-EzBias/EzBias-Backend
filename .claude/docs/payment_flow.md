# Payment Flow

## Payment References
- Reference format: `PAY-{yyyyMMddHHmmss}-{userId}`.
- Transfer content format: `EZB-{userId}-{HHmmss}`.
- Both order payments and auction deposit payments use the same reference/transfer content patterns.
- SePay reference extraction normalizes alphanumeric characters, finds `PAY`, expects at least 14 timestamp digits plus user ID, and reconstructs `PAY-{ts}-{userId}`.

## SePay Integration
- `SePayClient` calls `GET /userapi/transactions/list?account_number={AccountNumber}&limit={DefaultLimit}` against `SePay:BaseUrl`, default `https://my.sepay.vn`.
- Authentication uses `Authorization: Bearer {SePay:ApiToken}`.
- `SePay:DefaultLimit` defaults to `200`.
- HTTP 429 reads `x-sepay-userapi-retry-after` when present and returns a retry hint.

## Pull-Based Transaction Matching
- Webhook does not blindly trust payload amount as payment confirmation.
- After webhook reference mapping, `ConfirmBySePayPullAsync` pulls recent SePay transactions.
- A transaction matches when `AmountIn` equals `Payment.Amount` within `0.01` and normalized transaction content contains either `Payment.Reference` or `Payment.TransferContent`.
- On match, `Payment.ProviderTxnId`, `Payment.Payload`, and `Payment.UpdatedAt` are recorded before internal confirmation.

## Webhook Signature Verification
- `POST /api/payments/webhook` is `[AllowAnonymous]`.
- Headers: `X-SePay-Signature`, `X-SePay-Timestamp`.
- If `SePay:WebhookSecret` is empty, verification returns true.
- If configured, signature must equal `sha256={HMACSHA256("{timestamp}.{rawBody}", WebhookSecret)}` using fixed-time comparison.
- Non-`in` `TransferType` values are rejected.

## Order Payment Flow
- `POST /api/payments` requires auth and accepts order IDs.
- Orders must belong to caller and be `Pending`; an order cannot already have a payment.
- Payment amount is the sum of order totals. For a single auction order, a held winner deposit may reduce amount due while keeping `Order.Total` as final price.
- `Payment.Type = Order`, `Status = Pending`, `Currency = VND`.
- Confirmation marks payment `Paid`, sets `PaidAt`, marks each order `Paid`, notifies seller with `OrderPlaced`, and decrements product stock for cart orders.
- For auction orders, confirmation moves auction `EndedPendingPayment -> Sold`, releases product auction flag, and applies winner deposit `Held -> Applied`.

## Deposit Payment Flow
- Deposit initiation creates `Payment.Type = AuctionDeposit`, `Status = Pending`, amount equal to `Auction.RequiredDepositAmount`.
- Deposit payments have no `PaymentOrder`, no order escrow, and no commission.
- Confirmation first saves `Payment.Status = Paid`, then calls `ConfirmDepositAsync`.
- `ConfirmDepositAsync` finds the linked `AuctionDeposit`, transitions `PendingPayment -> Held`, records `HeldAt`, and queues `DepositConfirmed`.

## Escrow Recording
- Order payment confirmation creates escrow `IN` records once per paid order if no hold exists for the payment.
- `EscrowTransaction.Type = IN` amount is `Order.Total`, linked to payment.
- Delivered order finalization creates escrow `OUT` for seller net amount and creates a pending `Payout` when one does not already exist.
- `FinalizeOrderPayoutAsync` uses existing `CommissionTransaction.SellerNetAmount` when present; otherwise it falls back to `Order.Total`.

## Commission Recording
- Commission is recorded only for order payments, not auction deposit payments.
- `ConfiguredCommissionRateProvider` clamps `Commission:RatePercent` to 5-10%; default option value is `8`.
- `CommissionAmount = Math.Round(Order.Total * ratePercent / 100m, 2, MidpointRounding.AwayFromZero)`.
- `SellerNetAmount = Order.Total - CommissionAmount`.
- Duplicate commission records are avoided by checking `ExistsByPaymentIdAsync`.

## Manual Confirmation
- `POST /api/payments/{paymentId}/manual-confirm` requires `Admin`.
- It sets a `MANUAL-{adminId}-{yyyyMMddHHmmss}` provider transaction ID if missing, writes a manual payload, then runs the same internal confirmation path.
