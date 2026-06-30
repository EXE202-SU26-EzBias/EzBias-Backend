# Domain Model

## Conventions
- Primary IDs are `long` unless stated otherwise; `Fandom.Id` is a string slug.
- Money is `decimal`; most persisted money columns use `numeric(18,2)`. Commission rate uses `numeric(5,2)`, seller rating uses `numeric(3,2)`.
- Timestamps are `DateTimeOffset` and map to PostgreSQL `timestamptz`.
- Enums are persisted as strings via EF `.HasConversion<string>()`.
- JSON snapshots are stored as strings mapped to `jsonb`: `Order.AddressSnap`, `Payment.Payload`, `Notification.Meta`.
- Soft delete fields exist on `User.DeletedAt` and `Product.DeletedAt`.
- Snapshot/denormalized fields: `OrderItem.ProductName`, `OrderItem.ProductImage`, `Bid.UsernameSnap`, `Bid.AvatarSnap`, `Bid.AvatarBgSnap`, `Order.AddressSnap`, `CommissionTransaction` monetary snapshot fields.

## Entities
- `User`: account, seller profile, bank info, verification timestamps, role, soft delete. Related to products, cart, wishlist, follows, auctions, bids, orders, payments, payouts, escrows, notifications, refresh tokens, boosts, disputes, OTPs.
- `Fandom`: slug `Id`, name, active flag. Has many products.
- `Product`: seller, fandom, artist/name/type, condition, price, stock, description, primary image, auction flag, status, version, view count, boost state, soft delete. Has images, photocard detail, auctions, boosts, reviews.
- `ProductImage`: product image URL and sort order.
- `PhotocardDetail`: one-to-one product detail with member, album series, version, POB flag.
- `ProductBoost`: product/user boost window with `BoostStatus`.
- `ProductReview`: product/user rating with stars and optional comment.
- `CartItem`: user/product quantity.
- `Wishlist`: user/product join entity.
- `SellerFollow`: follower/seller join entity.
- `Auction`: product, seller, floor/reserve/current/final prices, required deposit, urgency/proof flags, extension settings, status, winner, end/deadline timestamps, 5-minute reminder flag, relist pointer. Has bids, orders, deposits.
- `Bid`: auction/user amount, winning flag, user snapshot fields, placed timestamp.
- `AuctionDeposit`: auction/user amount, `DepositState`, linked payment/refund, held/applied/forfeited/refunded timestamps, notification delivery flag, last error, forfeit retry count. Uses PostgreSQL `xmin` concurrency token and unique active deposit index for `(UserId, AuctionId)` while state is `PendingPayment` or `Held`.
- `Order`: buyer, seller, source, optional auction, total, status, address snapshot, shipping/delivery/completion fields. Has items, payment joins, escrows, commission, payout, dispute, refunds.
- `OrderItem`: order line with optional product FK, denormalized product name/image, quantity, unit price, computed subtotal.
- `Payment`: user, `PaymentType`, amount, currency, `PaymentStatus`, reference, transfer content, provider transaction ID, provider payload, paid timestamp. Has payment-order joins, escrows, commissions, refunds, auction deposits.
- `PaymentOrder`: payment/order many-to-many join.
- `EscrowTransaction`: order/seller escrow movement with `EscrowType`, amount, optional payment/payout.
- `CommissionTransaction`: order/payment/seller commission record with gross, rate, commission amount, seller net, currency.
- `Payout`: order/seller payout amount, status, bank transfer ref, paid timestamp. Has escrow OUT records.
- `Dispute`: order, initiator, reason, status, admin note, resolved timestamp. Has dispute items and refunds.
- `DisputeItem`: dispute/order-item requested and approved quantities plus note.
- `Refund`: payment with optional order/dispute, amount, reason, status, provider ref, processed timestamp.
- `Notification`: user, type, title, body, JSON meta, read status/timestamp.
- `RefreshToken`: user, SHA256 token hash, revoked flag/timestamps, expiry, optional device info.
- `OtpVerification`: user, channel, purpose, BCrypt-hashed code, used flag, expiry.
- `Conversation`: buyer, seller, optional product/order, last message timestamp. Has messages.
- `Message`: conversation sender, content, sent timestamp, read flag.
- `CallSession`: conversation, caller, callee, `CallSessionStatus`, created/answered/ended timestamps.
- `ContactMessage`: contact form name, email, subject, message, read flag.

## Enums
- `AuctionStatus`: `Draft`, `Live`, `Extended`, `EndedNoWinner`, `EndedPendingPayment`, `WinnerFailed`, `Sold`, `Canceled`
- `DepositState`: `PendingPayment`, `Held`, `Applied`, `Forfeited`, `Refunded`, `Failed`
- `OrderStatus`: `Pending`, `Paid`, `Processing`, `Shipped`, `Delivered`, `ReturnRequested`, `Completed`, `Canceled`, `Refunded`
- `OrderSource`: `Cart`, `Auction`
- `PaymentType`: `Order`, `AuctionDeposit`
- `PaymentStatus`: `Pending`, `Paid`, `Failed`, `Refunded`
- `EscrowType`: `IN`, `OUT`
- `PayoutStatus`: `Pending`, `Approved`, `Rejected`
- `RefundStatus`: `Pending`, `Completed`, `Failed`
- `DisputeStatus`: `Open`, `UnderReview`, `ResolvedBuyer`, `ResolvedSeller`, `Closed`
- `NotificationType`: `Outbid`, `AuctionWon`, `AuctionExpired`, `AuctionEndingSoon`, `OrderPlaced`, `OrderShipped`, `OrderDelivered`, `PayoutPaid`, `DisputeOpened`, `DisputeResolved`, `UserVerified`, `OrderConfirmed`, `NewMessage`, `DepositConfirmed`, `DepositRefundInitiated`, `DepositForfeited`, `DisputeRefundCompleted`, `DepositPendingReview`, `DisputePendingReview`
- `UserRole`: `User`, `Admin`
- `ProductStatus`: `Active`, `SoldOut`, `Archived`
- `ProductCondition`: `New`, `LikeNew`, `Good`, `Fair`
- `BoostStatus`: `Active`, `Expired`, `Canceled`
- `SubStatus`: `Active`, `Expired`, `Canceled`
- `OtpPurpose`: `EmailVerification`, `PhoneVerification`, `PasswordReset`
- `OtpChannel`: `Email`, `Sms`
- `CallSessionStatus`: `Ringing`, `Accepted`, `Rejected`, `Ended`, `Missed`, `Failed`
