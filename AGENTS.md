# EzBias Backend Guide

## Project Overview
EzBias is a Vietnamese K-pop merchandise marketplace backend for buying and selling photocards and merchandise grouped by fandom. It supports fixed-price cart checkout, live auctions with real-time bidding, SePay bank-transfer payments, escrow holds, seller payouts, disputes, refunds, notifications, chat, and video calls.

## Tech Stack
- .NET SDK / ASP.NET Core: `net8.0`
- Entity Framework Core: `8.0.4`
- PostgreSQL provider: `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.2`
- PostgreSQL Docker image: `postgres:16`
- JWT bearer auth: `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.4`
- JWT token library: `System.IdentityModel.Tokens.Jwt 7.5.1`
- BCrypt password hashing: `BCrypt.Net-Next 4.0.3`
- Cloudinary uploads: `CloudinaryDotNet 1.26.2`
- Swagger/OpenAPI: `Swashbuckle.AspNetCore 6.6.2`
- Docker runtime image: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Docker build image: `mcr.microsoft.com/dotnet/sdk:8.0`

## Dev Commands
Run commands from `EzBias/` unless noted.

- Restore dependencies: `dotnet restore EzBias.sln`
- Build: `dotnet build EzBias.sln`
- Run API locally: `dotnet run --project EzBias.API/EzBias.API.csproj`
- Run with Docker: copy `.env.example` to `.env`, fill secrets, then `docker compose up --build`
- Run tests: `dotnet test EzBias.sln` (the solution currently contains no committed test project)
- Add EF migration: `dotnet ef migrations add <Name> -p EzBias.Infrastructure -s EzBias.API`
- Apply migrations: the API calls `db.Database.Migrate()` at startup in `Program.cs`

## Core Logic Summary
- Commission is recorded on paid order payments as `round(order.Total * clampedRate / 100, 2)`, with configured `Commission:RatePercent` clamped to 5-10% and defaulting to 8%.
- Auction deposits use `Auction.RequiredDepositAmount`; seller auction creation accepts a whole-VND required deposit between 0 and floor price. The deposit gate is disabled when `RequiredDepositAmount` is `0`.
- Auction close creates a pending winner order with temporary address `{}`; winner checkout later updates that order address and the held deposit reduces amount due for a single auction order.
- `DeliveredOrderFinalizeScheduler` completes delivered orders after `Order:DeliveredFinalizeScheduler:GraceDays` (default 3) when no open dispute/pending refund blocks finalization, then creates seller payout and escrow OUT records.
- Buyer disputes are item-level, allowed only while an order is `Delivered` and inside the 3-day delivery grace window; admin approval creates a pending refund that is completed by a separate refund-payment action.

## Key Constraints
- Keep Domain pure: entities, enums, and repository interfaces live in `EzBias.Domain`; EF, auth, external clients, and SignalR live outside Domain.
- Application services use repositories plus `IUnitOfWork`; do not call `EzBiasDbContext.SaveChangesAsync` directly from Application.
- `IUnitOfWork` is registered as `UnitOfWork`; notification rows are dispatched by `NotificationDispatchScheduler`/`NotificationDispatchProcessor` through `IRealtimeNotifier` after persistence.
- State-changing application methods use `Result`/`Result<T>` and let controllers map errors to HTTP responses. Read-only list/query methods may return DTOs directly.
- Monetary values are `decimal` and persisted as `numeric(18,2)` unless a config explicitly uses another precision.
- Timestamps are `DateTimeOffset` and persisted as PostgreSQL `timestamptz`.
- Soft delete currently uses nullable `DeletedAt` on `User` and `Product`; do not replace it with hard delete for user/product flows.
- CORS origins are fixed in `Program.cs`: `http://localhost:5173`, `https://ez-bias-frontend.vercel.app`, `http://ezbias.io.vn`, and `https://ezbias.io.vn`.
- SignalR hubs accept JWT via `?access_token=` only for paths under `/hubs`.
- Non-winner auction deposit auto-refund is intentionally disabled in `AuctionCloseScheduler`; admins process those refunds manually.

## Additional Documentation
| Topic | File |
| --- | --- |
| Architecture | [.claude/docs/architecture.md](.claude/docs/architecture.md) |
| Domain model | [.claude/docs/domain_model.md](.claude/docs/domain_model.md) |
| Auction lifecycle | [.claude/docs/auction_lifecycle.md](.claude/docs/auction_lifecycle.md) |
| Payment flow | [.claude/docs/payment_flow.md](.claude/docs/payment_flow.md) |
| Realtime | [.claude/docs/realtime.md](.claude/docs/realtime.md) |
| Auth | [.claude/docs/auth.md](.claude/docs/auth.md) |
| Configuration | [.claude/docs/configuration.md](.claude/docs/configuration.md) |
