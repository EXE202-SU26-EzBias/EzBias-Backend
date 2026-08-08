# Architecture

## Solution Layout
- `EzBias.Domain`: pure domain model. Contains entities, enums, repository interfaces, and `IUnitOfWork`.
- `EzBias.Application`: use-case layer. Contains feature services, DTO contracts, payment/deposit policies, notification factory/dispatcher abstractions, and realtime abstraction interfaces.
- `EzBias.Infrastructure`: persistence and infrastructure implementations. Contains EF Core `EzBiasDbContext`, entity configurations, migrations, repositories, notification dispatch persistence, JWT token service, and BCrypt password hasher.
- `EzBias.API`: composition root and delivery layer. Contains controllers, SignalR hubs, hosted services, external integrations, `Program.cs`, Dockerfile, and app settings.

## Dependency Rules
- Domain has no project references.
- Application references Domain only.
- Infrastructure references Application and Domain.
- API references Application, Domain, and Infrastructure and wires everything in DI.
- Keep framework-specific concerns out of Domain. EF mapping belongs in `EzBias.Infrastructure.Persistence.Configurations`; HTTP, SignalR, hosted services, Cloudinary, Brevo, and SePay belong outside Domain.

## Runtime Composition
- `Program.cs` clears default configuration sources, then loads `appsettings.json`, optional environment-specific appsettings, and environment variables.
- Controllers, SignalR, response compression, Swagger, JWT bearer auth, CORS, EF Core/Npgsql, repositories, application services, hosted services, and named HTTP clients are registered in API.
- Named HTTP clients are `"SePay"` and `"Brevo"`.
- Swagger is enabled when the app is in Development or `Swagger:Enabled` is `true`.
- CORS is hard-coded for `http://localhost:5173`, `https://ez-bias-frontend.vercel.app`, `http://ezbias.io.vn`, and `https://ezbias.io.vn`.

## API Areas
- Public catalog: `api/catalog/products`, `api/catalog/products/{id}`, `api/catalog/fandoms`.
- Auth: `api/auth/register`, login, refresh, logout, forgot/reset password, email verification, `me`.
- User profile: `api/users/me`, profile update, and anonymous cleanup of unverified users by email.
- Seller products: authenticated `api/products` CRUD with Cloudinary-backed multipart image upload.
- Cart and checkout: `api/cart`, `api/orders`, `api/payments`.
- Seller fulfillment: `api/seller/orders`, `api/seller/dashboard`, `api/seller/payouts`.
- Auctions: public `api/auctions` reads and authenticated bidding/deposit/payment/post-flow endpoints.
- Admin: dashboards, users, deposits, payouts, disputes, manual payment confirmation, and review moderation.
- Collaboration: notifications, conversations/chat, chat image upload, and video calls.
- Utility endpoints: `api/contact` stores contact messages through `IContactApplicationService`. `api/debug/*` exposes development/secret-gated seed, reset, and config helpers; `GET /api/debug/health` is a stateless, read-only operational probe returning `status`, `timestamp`, and `uptime`, independent of hosted services.

## Repository Pattern
- Repository interfaces live in `EzBias.Domain.Interfaces`.
- Implementations live in `EzBias.Infrastructure.Repositories`.
- Application services receive repository interfaces and `IUnitOfWork` through constructor injection.
- Repositories stage changes with `Add`, `AddRange`, `Remove`, `RemoveRange`, or entity mutation; application services call `IUnitOfWork.SaveChangesAsync`.
- The intentional API-layer persistence exception is `DebugController`, which handles development-only reset/seed operations directly through `EzBiasDbContext`.

## Unit of Work and Notifications
- `UnitOfWork` is the runtime implementation of `IUnitOfWork` and wraps `EzBiasDbContext.SaveChangesAsync` plus transaction helpers.
- Application services persist `Notification` entities through `INotificationRepository`; `NotificationDispatchScheduler` claims pending rows through the repository and `NotificationDispatchProcessor` pushes them through `IRealtimeNotifier` with lease, retry, and backoff handling.
- `NotificationRepository` intentionally owns the atomic `FOR UPDATE SKIP LOCKED` lease transaction for dispatch claims; this is a dispatch-infrastructure exception, not the normal Application persistence pattern.
- Auction, chat, and video-call realtime adapters are post-save best-effort delivery boundaries: persistence happens first, and transport failures are logged without turning a committed write into a false request failure.
- When changing save behavior, preserve the persistence-first ordering unless the failure model is intentionally redesigned.

## Service Result Pattern
- State-changing application service methods generally return `Result` or `Result<T>` with `ApplicationErrorCode`; read-only list/query methods may return DTOs directly, and schedulers return run summaries.
- Controllers convert these results to HTTP responses. Keep domain/application code free from ASP.NET `IActionResult`.
- Controllers branch on stable error strings such as `"Forbidden."`, `"Payment not found."`, `"Auction not found."`, and `"Order not found."`.

## Hosted Services
- `AuctionCloseScheduler`: every configured interval, sends near-end reminders, closes ended auctions, creates pending auction orders for winners, cancels unpaid winner orders after 24 hours, and forfeits winner deposits. Non-winner auto-refund code is deliberately disabled.
- `DeliveredOrderFinalizeScheduler`: completes delivered orders after the configured grace period when no open dispute or pending refund blocks finalization, then creates escrow OUT and pending seller payout records.
- `NotificationDispatchScheduler`: claims pending notification rows and delivers them through the realtime adapter with lease/retry handling.
- Hosted services create a DI scope on each polling tick before resolving scoped repositories/services.

## Startup and Seed Data
- At startup the API runs `db.Database.Migrate()`.
- Seed order in `Program.cs`: product seed, auction seed, sales seed, product review seed, transaction seed.
- The debug reset endpoint truncates application tables with cascade, restarts identities, then re-runs the same seed sequence when `Debug:ResetSecret` matches.

## Cross-Cutting Conventions
- Monetary values use `decimal`; most money columns are `numeric(18,2)`.
- Timestamps use `DateTimeOffset` and PostgreSQL `timestamptz`.
- Enums are stored as strings.
- Soft delete uses nullable `DeletedAt` on `User` and `Product`.
- JWT query-string extraction is allowed only for paths under `/hubs`.
