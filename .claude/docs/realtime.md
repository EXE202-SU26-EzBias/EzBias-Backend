# Realtime

## Hub Routes
| Hub | Route | Auth | Groups | Primary events |
| --- | --- | --- | --- | --- |
| `NotificationHub` | `/hubs/notifications` | `[Authorize]` | `user-{userId}` | `ReceiveNotification` |
| `AuctionHub` | `/hubs/auction` | Public | `auction-{auctionId}` | `BidPlaced` |
| `ChatHub` | `/hubs/chat` | `[Authorize]` | `chat-user-{userId}` | `ReceiveMessage`, `ConversationRead` |
| `CallHub` | `/hubs/calls` | `[Authorize]` | `call-user-{userId}` | `IncomingCall`, `CallAccepted`, `CallRejected`, `CallEnded`, `WebRtcOffer`, `WebRtcAnswer`, `IceCandidate` |

## JWT for Hubs
- JWT bearer auth reads `?access_token=...` only when the request path starts with `/hubs`.
- Authenticated hubs derive user ID from `ClaimTypes.NameIdentifier` or `sub`.
- `NameClaimType = "sub"` and `RoleClaimType = "role"` are configured in JWT bearer options.
- `AuctionHub` is intentionally public so anonymous viewers can subscribe to auction rooms.

## Notification Dispatch Pattern
- Application services add `Notification` entities through `INotificationRepository`.
- `IUnitOfWork` is registered as `NotificationDispatchingUnitOfWork`.
- On save, the UoW captures pending added notifications, saves EF changes, then pushes each persisted notification to `NotificationHub.UserGroup(notification.UserId)`.
- Payload fields: `id`, `type`, `title`, `body`, `meta`, `isRead`, `createdAt`.
- Persistence happens before push. The current implementation awaits SignalR sends and does not catch push exceptions, so a dispatch failure can bubble after the database commit has already succeeded.
- REST notification reads and read-state updates are available through `api/notifications`.

## AuctionHub Bid Push Flow
- Clients call `JoinAuction(auctionId)` and `LeaveAuction(auctionId)`.
- `POST /api/auctions/{auctionId}/bids` requires auth and uses `AuctionBiddingApplicationService.PlaceBidAsync`.
- After a successful bid, `AuctionsController` sends `BidPlaced` to `auction-{auctionId}`.
- Bid payload includes `auctionId`, `bidId`, `amount`, `currentBid`, `status`, and `placedAt`.
- Outbid notifications are persisted and pushed through the notification UoW path, separate from the auction room broadcast.

## Chat Flow
- On connect, authenticated clients join `chat-user-{userId}`.
- `POST /api/conversations` starts or resumes a conversation. The caller is stored as buyer and the counterpart as seller; `(BuyerId, SellerId)` is unique.
- `POST /api/conversations/{id}/messages` persists the message, updates `LastMessageAt`, creates a `NewMessage` notification, saves through the notification UoW, then directly pushes `ReceiveMessage` to the recipient's chat group.
- Messages must be non-empty and at most 2000 characters.
- Image messages are represented as URLs in `Message.Content`; notification/conversation previews use an image label when the content looks like a Cloudinary or image URL.
- `GET /api/conversations/{id}/messages` uses cursor pagination with `before` and clamps `pageSize` to `1..100`.
- `PUT /api/conversations/{id}/read` marks unread messages for the caller and pushes `ConversationRead` to the original sender's chat group.
- `POST /api/conversations/{id}/upload-image` validates membership, accepts multipart JPEG/JPG/PNG/GIF/WebP up to 5 MB at the controller layer, and uploads through the shared Cloudinary image uploader. The uploader itself currently accepts JPEG/PNG/WebP.

## CallHub Flow
- On connect, authenticated clients join `call-user-{userId}`.
- Calls are REST-created under `api/conversations/{conversationId}/calls`.
- `StartCallAsync` requires conversation membership, rejects a second active call for the conversation, creates a `Ringing` call, saves, then pushes `IncomingCall` to the callee.
- `AcceptCallAsync` can only be called by the callee while status is `Ringing`; it sets `Accepted` and pushes `CallAccepted` to the caller.
- `RejectCallAsync` can only be called by the callee while status is `Ringing`; it sets `Rejected`, sets `EndedAt`, and pushes `CallRejected` to the caller.
- `EndCallAsync` can be called by either participant while status is `Ringing` or `Accepted`; it sets `Ended`, sets `EndedAt`, and pushes `CallEnded` to the other participant.
- `CallHub.SendOffer`, `SendAnswer`, and `SendIceCandidate` relay WebRTC signals to the target participant group.
- Signal relay validates caller membership, validates that `targetUserId` is the other participant, and rejects calls outside `Ringing` or `Accepted`.
