# Architecture

## Solution Layout
- `EzBias.Domain`: pure domain model. Contains entities, enums, and repository/unit-of-work interfaces.
- `EzBias.Application`: use-case layer. Contains feature services, DTO contracts, service interfaces, notification factory/dispatcher abstractions, payment/deposit policies, and realtime abstraction interfaces.
- `EzBias.Infrastructure`: persistence and infrastructure implementations. Contains EF Core `EzBiasDbContext`, entity configurations, migrations, repositories, `UnitOfWork`, `NotificationDispatchingUnitOfWork`, JWT token service, and BCrypt password hasher.
- `EzBias.API`: composition root and delivery layer. Contains controllers, SignalR hubs, background services, external integrations, `Program.cs`, Dockerfile, and app settings.

## Dependency Rules
- Domain has no project references.
- Application references Domain only.
- Infrastructure references Application and Domain.
- API references Application, Domain, and Infrastructure and wires everything in DI.
- Keep framework-specific concerns out of Domain. EF mapping belongs in `EzBias.Infrastructure.Persistence.Configurations`; HTTP, SignalR, and hosted services belong in API.

## Repository Pattern
- Repository interfaces live in `EzBias.Domain.Interfaces`.
- Implementations live in `EzBias.Infrastructure.Repositories`.
- Application services receive repository interfaces and `IUnitOfWork` through constructor injection.
- Repositories stage changes with `Add`, `AddRange`, `Remove`, or entity mutation; Application services call `IUnitOfWork.SaveChangesAsync`.

## Unit of Work Decorator Pattern
- `UnitOfWork` is the minimal wrapper around `EzBiasDbContext.SaveChangesAsync`.
- Runtime DI registers `IUnitOfWork` as `NotificationDispatchingUnitOfWork`, not `UnitOfWork`.
- `NotificationDispatchingUnitOfWork.SaveChangesAsync` captures newly added `Notification` entities before saving, calls EF `SaveChangesAsync`, then pushes those notifications through `IRealtimeNotifier`.
- Realtime notification dispatch is post-save. If changing dispatch behavior, preserve the persistence-first ordering unless you intentionally redesign the failure model.

## Service Result Tuple Pattern
- Application services generally return tuples such as `(bool Success, string? Error, T? Data)` or `(bool Success, string? Error)`.
- Controllers convert these results to HTTP responses. Keep domain/application code free from ASP.NET `IActionResult`.
- Use stable error strings if a controller branches on them, for example `"Forbidden."`, `"Payment not found."`, or `"Auction not found."`.

## DI Scope Rules
- `EzBiasDbContext`, repositories, application services, external integration services, realtime adapters, auth services, `IUnitOfWork`, `ISePayClient`, and `ISePayWebhookVerifier` are scoped.
- `INotificationFactory` is singleton.
- `AuctionCloseScheduler`, `DeliveredOrderFinalizeScheduler`, and `KeepAliveScheduler` are hosted services. They create a scope on each polling tick before resolving scoped services.
- HTTP clients are registered by name: `"SePay"`, `"Brevo"`, and `"KeepAlive"`.

## Startup Behavior
- `Program.cs` clears default configuration sources, loads `appsettings.json`, optional environment-specific appsettings, then environment variables.
- `Program.cs` runs EF migrations and seed data at startup with `db.Database.Migrate()` followed by product, auction, sales, review, and transaction seeders.
- Swagger is enabled in Development or when `Swagger:Enabled` is `true`.
