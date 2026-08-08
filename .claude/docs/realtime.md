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
- `IUnitOfWork` is registered as `UnitOfWork`.
- Application services add notification rows through `INotificationRepository` and save through the UoW. `NotificationDispatchScheduler` claims pending rows with a lease; `NotificationDispatchProcessor` pushes each persisted notification to `NotificationHub.UserGroup(notification.UserId)` through `IRealtimeNotifier`.
- Payload fields: `id`, `type`, `title`, `body`, `meta`, `isRead`, `createdAt`.
- Persistence happens before push. Notification delivery retries with backoff; a permanently failed row is retained for operational inspection.
- REST notification reads and read-state updates are available through `api/notifications`.

## AuctionHub Bid Push Flow
- Clients call `JoinAuction(auctionId)` and `LeaveAuction(auctionId)`.
- `POST /api/auctions/{auctionId}/bids` requires auth and uses `AuctionBiddingApplicationService.PlaceBidAsync`.
- After a successful bid, `AuctionBiddingApplicationService` calls `IAuctionRealtime`, whose SignalR adapter sends `BidPlaced` to `auction-{auctionId}`.
- Bid payload includes `auctionId`, `bidId`, `amount`, `currentBid`, `status`, and `placedAt`.
- Outbid notifications are persisted and dispatched through the notification scheduler, separate from the auction room broadcast.

## Chat Flow
- On connect, authenticated clients join `chat-user-{userId}`.
- `POST /api/conversations` starts or resumes a conversation. The caller is stored as buyer and the counterpart as seller; `(BuyerId, SellerId)` is unique.
- `POST /api/conversations/{id}/messages` persists the message, updates `LastMessageAt`, saves through `IUnitOfWork`, then calls `IChatRealtime` to push `ReceiveMessage` to the recipient's chat group. Chat messages do not create notification rows; unread state is tracked on `Message` and the conversation summary. Chat transport failures are logged as best-effort post-save failures.
- Messages must be non-empty and at most 2000 characters.
- Image messages are represented as URLs in `Message.Content`; notification/conversation previews use an image label when the content looks like a Cloudinary or image URL.
- `GET /api/conversations/{id}/messages` uses cursor pagination with `before` and clamps `pageSize` to `1..100`.
- `PUT /api/conversations/{id}/read` marks unread messages for the caller and calls `IChatRealtime` to push `ConversationRead` to the original sender's chat group. When an active frontend thread receives `ReceiveMessage`, it immediately invokes this endpoint for unread incoming messages. Transport failures are logged without undoing the persisted read state.
- The frontend shows `Đã gửi` or `Đã xem` only for the latest outgoing message in the loaded thread; older outgoing messages and incoming messages have no delivery label.
- `POST /api/conversations/{id}/upload-image` validates membership, accepts multipart JPEG/JPG/PNG/GIF/WebP up to 5 MB at the controller layer, and uploads through the shared Cloudinary image uploader. The uploader itself currently accepts JPEG/PNG/WebP.

## CallHub Flow
- On connect, authenticated clients join `call-user-{userId}`.
- Calls are REST-created under `api/conversations/{conversationId}/calls`.
- `StartCallAsync` requires conversation membership, rejects a second active call for the conversation, creates a `Ringing` call, saves, then calls `IVideoCallRealtime` to push `IncomingCall` to the callee.
- `AcceptCallAsync` can only be called by the callee while status is `Ringing`; it sets `Accepted` and calls the realtime adapter to push `CallAccepted` to the caller.
- `RejectCallAsync` can only be called by the callee while status is `Ringing`; it sets `Rejected`, sets `EndedAt`, and calls the realtime adapter to push `CallRejected` to the caller.
- `EndCallAsync` can be called by either participant while status is `Ringing` or `Accepted`; it sets `Ended`, sets `EndedAt`, and calls the realtime adapter to push `CallEnded` to the other participant. Call transport failures are logged as best-effort post-save failures.
- `CallHub.SendOffer`, `SendAnswer`, and `SendIceCandidate` relay WebRTC signals to the target participant group.
- Signal relay validates caller membership, validates that `targetUserId` is the other participant, and rejects calls outside `Ringing` or `Accepted`.
