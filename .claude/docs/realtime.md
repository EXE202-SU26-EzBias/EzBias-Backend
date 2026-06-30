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

## Notification Dispatch Pattern
- Application services add `Notification` entities through `INotificationRepository`.
- `IUnitOfWork` is registered as `NotificationDispatchingUnitOfWork`.
- On save, the UoW captures pending added notifications, saves via EF, then pushes each persisted notification to `NotificationHub.UserGroup(notification.UserId)`.
- Payload fields: `id`, `type`, `title`, `body`, `meta`, `isRead`, `createdAt`.
- Treat push as a post-save side effect. Persistence is the source of truth.

## AuctionHub Bid Push Flow
- Clients call `JoinAuction(auctionId)` and `LeaveAuction(auctionId)`.
- `POST /api/auctions/{auctionId}/bids` requires auth and uses `AuctionBiddingApplicationService.PlaceBidAsync`.
- After a successful bid, `AuctionsController` sends `BidPlaced` to `auction-{auctionId}`.
- Bid payload includes `auctionId`, `bidId`, `amount`, `currentBid`, `status`, and `placedAt`.

## ChatHub Flow
- On connect, authenticated clients join `chat-user-{userId}`.
- `SignalRChatRealtime.PushMessageAsync` sends `ReceiveMessage` to the recipient's group.
- `SignalRChatRealtime.PushConversationReadAsync` sends `ConversationRead` to the sender's group with `conversationId` and `readByUserId`.

## CallHub Flow
- On connect, authenticated clients join `call-user-{userId}`.
- `SignalRVideoCallRealtime` pushes call lifecycle events: `IncomingCall`, `CallAccepted`, `CallRejected`, `CallEnded`.
- `CallHub.SendOffer`, `SendAnswer`, and `SendIceCandidate` relay WebRTC signals to the other participant.
- Signal relay validates caller membership in the call, validates target user, and rejects calls not in `Ringing` or `Accepted`.
