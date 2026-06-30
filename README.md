# EzBias Backend

EzBias is a Vietnamese K-pop merchandise marketplace backend. It supports fixed-price product checkout, live auctions with real-time bidding, SePay bank-transfer payment confirmation, escrow tracking, seller payouts, disputes, refunds, notifications, chat, and video calls.

## Repository Structure

```text
EzBias/
  EzBias.API/             ASP.NET Core API, controllers, SignalR hubs, integrations, hosted services
  EzBias.Application/     Use-case services, DTOs, policies, realtime/payment/auth abstractions
  EzBias.Domain/          Entities, enums, repository interfaces, unit-of-work interface
  EzBias.Infrastructure/  EF Core DbContext, configurations, migrations, repositories, auth implementations
  docker-compose.yml      PostgreSQL + API runtime
  .env.example            Local Docker environment template
```

Additional agent/developer documentation is in:

- [CLAUDE.md](CLAUDE.md)
- [.claude/docs/architecture.md](.claude/docs/architecture.md)
- [.claude/docs/domain_model.md](.claude/docs/domain_model.md)
- [.claude/docs/auction_lifecycle.md](.claude/docs/auction_lifecycle.md)
- [.claude/docs/payment_flow.md](.claude/docs/payment_flow.md)
- [.claude/docs/realtime.md](.claude/docs/realtime.md)
- [.claude/docs/auth.md](.claude/docs/auth.md)
- [.claude/docs/configuration.md](.claude/docs/configuration.md)

## Tech Stack

- .NET 8 / ASP.NET Core
- Entity Framework Core 8.0.4
- PostgreSQL 16
- Npgsql EF Core provider 8.0.2
- SignalR
- JWT bearer authentication
- BCrypt password hashing
- SePay banking transaction integration
- Brevo transactional email integration
- Cloudinary product image uploads
- Docker Compose

## Prerequisites

- .NET SDK 8
- Docker Desktop or compatible Docker runtime
- PostgreSQL 16 if running without Docker
- `dotnet-ef` CLI for migrations:

```powershell
dotnet tool install --global dotnet-ef
```

## Local Setup

Run commands from the `EzBias/` directory:

```powershell
cd EzBias
dotnet restore EzBias.sln
dotnet build EzBias.sln
dotnet run --project EzBias.API/EzBias.API.csproj
```

The API launch profile uses:

- HTTP: `http://localhost:5003`
- HTTPS: `https://localhost:7265`
- Swagger path: `/swagger`

The API applies EF migrations and seed data on startup through `Program.cs`.

## Docker Setup

From `EzBias/`:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Edit `.env` before running anything beyond local development. At minimum, replace JWT and integration secrets.

Default Docker services:

- `postgres`: PostgreSQL 16
- `ezbias-api`: ASP.NET Core API on `${API_PORT}:8080`

## Configuration

Configuration is loaded from:

1. `appsettings.json`
2. optional `appsettings.{Environment}.json`
3. environment variables

Use .NET double-underscore environment variables for nested keys:

```text
Jwt__SecretKey
ConnectionStrings__DefaultConnection
SePay__ApiToken
SePay__AccountNumber
SePay__WebhookSecret
Cloudinary__CloudName
```

Production deployments must change:

- Database credentials / connection string
- `Jwt__SecretKey`
- SePay API token, account number, and webhook secret
- Brevo API key/from email if email OTP is enabled
- Cloudinary keys if uploads are enabled
- Swagger exposure policy

Full configuration notes are in [.claude/docs/configuration.md](.claude/docs/configuration.md).

## Core Business Rules

- Commission rate defaults to 8% and is clamped to 5-10%.
- Commission amount is recorded as `round(Order.Total * RatePercent / 100, 2)`.
- Auction deposits are stored per auction as `Auction.RequiredDepositAmount`.
- Seller-created auctions accept a required deposit between 0 and floor price; 0 disables the deposit gate.
- Auction winner has 24 hours to pay after auction close.
- If the winner does not pay, the auction becomes `WinnerFailed` and the winner deposit is forfeited.
- Delivered orders auto-complete after 3 days when no open dispute or pending refund blocks finalization.
- Auto-complete creates seller payout and escrow OUT records.
- Non-winner auction deposit auto-refund is intentionally disabled; admins manually process refunds.

## Main Runtime Flows

### Fixed-Price Checkout

1. Buyer adds active products to cart.
2. Buyer creates pending orders grouped by seller.
3. Buyer creates a SePay payment for order IDs.
4. Webhook/pull matching confirms the bank transaction.
5. Payment confirmation marks orders paid, records escrow IN, records commission, and notifies sellers.
6. Seller ships; buyer confirms delivery.
7. Scheduler completes delivered orders after the grace period and creates payout records.

### Auction Checkout

1. Seller creates and publishes auction.
2. Bidder pays deposit if `RequiredDepositAmount > 0`.
3. Deposit payment confirmation moves deposit `PendingPayment -> Held`.
4. Bidder places bids while auction is live.
5. Scheduler closes auction and creates a pending auction order for the winner.
6. Winner pays within 24 hours; held deposit reduces amount due.
7. Payment confirmation moves auction to `Sold` and deposit `Held -> Applied`.
8. If the winner misses the deadline, scheduler marks `WinnerFailed` and forfeits the held deposit.

## Realtime

SignalR hubs:

| Hub | Route | Auth |
| --- | --- | --- |
| `NotificationHub` | `/hubs/notifications` | Required |
| `AuctionHub` | `/hubs/auction` | Public |
| `ChatHub` | `/hubs/chat` | Required |
| `CallHub` | `/hubs/calls` | Required |

Authenticated hubs support JWT via query string:

```text
/hubs/chat?access_token=<jwt>
```

More details are in [.claude/docs/realtime.md](.claude/docs/realtime.md).

## Database Migrations

Add a migration:

```powershell
dotnet ef migrations add <MigrationName> -p EzBias.Infrastructure -s EzBias.API
```

Apply migrations manually:

```powershell
dotnet ef database update -p EzBias.Infrastructure -s EzBias.API
```

The API also runs `db.Database.Migrate()` at startup.

## Tests

Run:

```powershell
dotnet test EzBias.sln
```

The current solution file does not include a committed test project. If adding tests, keep them in the solution and cover application service rules around payments, deposits, auctions, disputes, and payout finalization.

## Development Constraints

- Keep `EzBias.Domain` free of framework, EF, HTTP, SignalR, and external-service dependencies.
- Application services should use repository interfaces plus `IUnitOfWork`.
- Do not bypass `NotificationDispatchingUnitOfWork` when saving changes that may include notifications.
- Keep monetary values as `decimal`.
- Keep persisted timestamps as `DateTimeOffset` / PostgreSQL `timestamptz`.
- Preserve soft-delete semantics for `User.DeletedAt` and `Product.DeletedAt`.
- Do not re-enable non-winner deposit auto-refunds without an explicit product decision.
