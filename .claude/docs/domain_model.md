# Domain Model

## Persistence Conventions
- Primary IDs are `long` unless stated otherwise; `Fandom.Id` is a string slug.
- Money is `decimal`; most persisted money columns use `numeric(18,2)`. Commission rate uses `numeric(5,2)`, seller rating uses `numeric(3,2)`.
- Timestamps are `DateTimeOffset` and map to PostgreSQL `timestamptz`.
- Enums are persisted as strings with EF `.HasConversion<string>()`.
- JSON snapshots are stored as strings mapped to `jsonb`: `Order.AddressSnap`, `Payment.Payload`, and `Notification.Meta`.
- Soft delete fields exist on `User.DeletedAt` and `Product.DeletedAt`.
- Snapshot/denormalized fields include `OrderItem.ProductName`, `OrderItem.ProductImage`, `Bid.UsernameSnap`, `Bid.AvatarSnap`, `Bid.AvatarBgSnap`, `Order.AddressSnap`, and commission monetary fields.

## Users and Identity
- `User`: account identity, role, profile, address, avatar, bank information, email/phone verification timestamps, seller rating counters, soft delete, and relations to products, cart, auctions, bids, orders, payments, payouts, escrows, notifications, refresh tokens, disputes, and OTPs.
- `RefreshToken`: SHA256-hashed token, revocation state/timestamps, expiry, and optional device info.
- `OtpVerification`: user, channel, purpose, BCrypt-hashed code, used flag, expiry, and created timestamp.
- `UserRole`: `User`, `Admin`.

## Catalog and Products
- `Fandom`: slug `Id`, name, active flag, and products.
- `Product`: seller, fandom, artist/name/type, condition, price, stock, description, primary image, auction flag, status, version, soft delete, images, auctions, and reviews.
- `ProductImage`: product image URL and sort order.
- `ProductReview`: product/user rating with 1-5 stars and optional comment. `(ProductId, UserId)` is unique.
- `ProductStatus`: `Active`, `SoldOut`, `Archived`.
- `ProductCondition`: `New`, `LikeNew`, `Good`, `Fair`.

## Cart, Orders, and Fulfillment
- `CartItem`: user/product quantity. Cart service rejects normal auction products, but auction winners can add a won item through the auction cart endpoint.
- `Order`: buyer, seller, source, optional auction, total, status, address snapshot, carrier/tracking, shipped/delivered/completed timestamps, items, payment joins, escrows, commission, payout, dispute, and refunds.
- `OrderItem`: order line with optional product FK, denormalized product name/image, quantity, unit price, and subtotal.
- `PaymentOrder`: many-to-many join between payments and orders. It uses a composite key `(PaymentId, OrderId)`.
- `OrderSource`: `Cart`, `Auction`.
- `OrderStatus`: `Pending`, `Paid`, `Processing`, `Shipped`, `Delivered`, `ReturnRequested`, `Completed`, `Canceled`, `Refunded`.

## Payments, Escrow, Commission, Refunds, and Payouts
- `Payment`: user, type, amount, `char(3)` currency, status, reference, transfer content, provider transaction ID, provider payload, paid timestamp, order joins, escrow records, commission records, refunds, and auction deposits.
- `EscrowTransaction`: order/seller escrow movement with `EscrowType`, amount, optional payment, optional payout, and created timestamp.
- `CommissionTransaction`: one per paid order; stores order/payment/seller, gross amount, commission rate, commission amount, seller net amount, currency, and created timestamp.
- `Refund`: payment with optional order/dispute, amount, reason, status, provider reference, processed timestamp, and created timestamp.
- `Payout`: one per finalized order; seller payout amount, status, bank transfer reference, paid timestamp, order, seller, and escrow OUT records.
- `PaymentType`: `Order`, `AuctionDeposit`.
- `PaymentStatus`: `Pending`, `Paid`, `Failed`, `Refunded`.
- `EscrowType`: `IN`, `OUT`.
- `RefundStatus`: `Pending`, `Completed`, `Failed`.
- `PayoutStatus`: `Pending`, `Approved`, `Rejected`.

## Auctions and Deposits
- `Auction`: product, seller, floor/reserve/current/final prices, required deposit, urgency/proof flags, extension settings, status, winner, end/deadline timestamps, 5-minute reminder flag, relist pointer, bids, orders, and deposits.
- `Bid`: auction/user amount, winning flag, optional user snapshot fields, and placed timestamp. Current bid placement writes the user FK and amount; snapshot fields default to empty strings unless another flow populates them.
- `AuctionDeposit`: auction/user amount, `DepositState`, linked payment/refund, held/applied/forfeited/refunded timestamps, confirmation-delivery flag, last error, forfeit retry count, created/updated timestamps, and relations.
- `AuctionDeposit` uses PostgreSQL `xmin` optimistic concurrency and a filtered unique index on `(UserId, AuctionId)` for active states `PendingPayment` or `Held`.
- `AuctionStatus`: `Draft`, `Live`, `Extended`, `EndedNoWinner`, `EndedPendingPayment`, `WinnerFailed`, `Sold`, `Canceled`.
- `DepositState`: `PendingPayment`, `Held`, `Applied`, `Forfeited`, `Refunded`, `Failed`.

## Disputes
- `Dispute`: one per order, initiator, reason, status, admin note, resolved timestamp, items, and refunds.
- `DisputeItem`: disputed order item, requested quantity, approved quantity, note, and created timestamp. `(DisputeId, OrderItemId)` is unique.
- `DisputeStatus`: `Open`, `UnderReview`, `ResolvedBuyer`, `ResolvedSeller`, `Closed`.

## Notifications, Chat, Calls, and Contact
- `Notification`: user, type, title, body, JSON meta, read flag, created timestamp, and read timestamp.
- `Conversation`: unique buyer/seller pair, optional product/order context, last message timestamp, and messages.
- `Message`: conversation sender, content, sent timestamp, and read flag.
- `CallSession`: conversation, caller, callee, `CallSessionStatus`, created/answered/ended timestamps.
- `ContactMessage`: contact form name, email, subject, message, read flag, and created timestamp.
- `NotificationType`: `Outbid`, `AuctionWon`, `AuctionExpired`, `AuctionEndingSoon`, `OrderPlaced`, `OrderShipped`, `OrderDelivered`, `PayoutPaid`, `DisputeOpened`, `DisputeResolved`, `UserVerified`, `OrderConfirmed`, `NewMessage`, `DepositConfirmed`, `DepositRefundInitiated`, `DepositForfeited`, `DisputeRefundCompleted`, `DepositPendingReview`, `DisputePendingReview`.
- `CallSessionStatus`: `Ringing`, `Accepted`, `Rejected`, `Ended`, `Missed`, `Failed`.

## Important Indexes and One-to-One Constraints
- `ProductReview` is unique by `(ProductId, UserId)`.
- `Conversation` is unique by `(BuyerId, SellerId)`, so starting a conversation with a reversed business meaning creates or reuses only that pair in the stored buyer/seller order used by the service.
- `Dispute`, `Payout`, and `CommissionTransaction` are one-to-one with `Order`.
- `Payout.OrderId` and `CommissionTransaction.OrderId` are unique.
- `Dispute.OrderId` is unique; rejected disputes can be reopened by reusing the existing row.
- `AuctionDeposit` allows only one active `PendingPayment` or `Held` deposit per user/auction.
