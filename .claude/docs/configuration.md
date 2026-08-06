# Configuration

## Loading Order
- `Program.cs` clears default configuration sources.
- Loads `appsettings.json`.
- Loads optional `appsettings.{Environment}.json`.
- Loads environment variables.
- Environment variable mapping uses .NET double-underscore style, for example `Jwt__SecretKey`.

## appsettings Sections and Keys
- `ConnectionStrings:DefaultConnection`: PostgreSQL connection string. Default uses localhost `ezbias_db` with placeholder credentials.
- `Jwt:Issuer`: default `EzBias`.
- `Jwt:Audience`: default `EzBias.Client`.
- `Jwt:SecretKey`: default placeholder `CHANGE_ME_IN_ENV_OR_APPSETTINGS_DEVELOPMENT`.
- `Jwt:AccessTokenMinutes`: default `60`.
- `Jwt:RefreshTokenDays`: default `14`.
- `Auction:CloseScheduler:IntervalSeconds`: default `10`, minimum runtime value `1`.
- `Order:DeliveredFinalizeScheduler:IntervalSeconds`: default `60`, minimum runtime value `1`.
- `Order:DeliveredFinalizeScheduler:GraceDays`: default `3`, minimum runtime value `0`.
- `SePay:BaseUrl`: default `https://my.sepay.vn`.
- `SePay:ApiToken`: placeholder.
- `SePay:AccountNumber`: placeholder.
- `SePay:WebhookSecret`: option default empty; absent in checked-in appsettings.
- `SePay:DefaultLimit`: option default `200`; absent in checked-in appsettings.
- `Brevo:ApiKey`: placeholder.
- `Brevo:FromEmail`: placeholder.
- `Brevo:FromName`: default `EzBias`.
- `Commission:RatePercent`: default `8`, clamped at runtime to `5..10`.
- `Cloudinary:CloudName`: default empty.
- `Cloudinary:ApiKey`: default empty.
- `Cloudinary:ApiSecret`: default empty.
- `Cloudinary:Folder`: default `ezbias/products`.
- `Swagger:Enabled`: default `true`.
- `Debug:ResetSecret`: default empty.
- `Logging:LogLevel:Default`: default `Information`.
- `Logging:LogLevel:Microsoft.AspNetCore`: default `Warning`.
- `AllowedHosts`: default `*`.

## docker-compose Environment Mapping
- Compose reads `.env` and injects database settings into `postgres`: `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`.
- API listens on `ASPNETCORE_URLS=http://+:8080` and exposes host `${API_PORT}:8080`.
- API connection string is injected as `ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}`.
- JWT env mapping: `Jwt__Issuer=${JWT_ISSUER}`, `Jwt__Audience=${JWT_AUDIENCE}`, `Jwt__SecretKey=${JWT_SECRET}`, `Jwt__AccessTokenMinutes=${JWT_ACCESS_TOKEN_MINUTES}`, `Jwt__RefreshTokenDays=${JWT_REFRESH_TOKEN_DAYS}`.
- Brevo env mapping: `Brevo__ApiKey`, `Brevo__FromEmail`, `Brevo__FromName`.
- Commission env mapping: `Commission__RatePercent=${COMMISSION_RATE_PERCENT}`.
- Cloudinary env mapping: `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`, `Cloudinary__Folder`.
- `.env.example` does not define or inject SePay keys; configure `SePay__ApiToken`, `SePay__AccountNumber`, `SePay__WebhookSecret`, and optionally `SePay__DefaultLimit` in the deployment environment.

## Integration Behavior
- SePay requires both `ApiToken` and `AccountNumber`; otherwise payment pull matching returns an error.
- SePay webhook signature verification is disabled when `WebhookSecret` is empty.
- Brevo is considered configured when `ApiKey` and `FromEmail` are non-empty. If not configured, OTP emails are not sent and the OTP is logged.
- Cloudinary is considered configured when `CloudName`, `ApiKey`, and `ApiSecret` are non-empty. Product uploads fail with `"Cloudinary is not configured."` when missing.
- Shared Cloudinary upload validation allows JPEG, PNG, and WEBP images up to 5 MB. Chat upload controller also accepts GIF at the controller validation layer, but the shared uploader will reject GIF unless the uploader is extended.

## Fixed Code-Level Settings
- CORS origins are hard-coded in `Program.cs`: `http://localhost:5173` and `https://ez-bias-frontend.vercel.app`.
- JWT via query string is accepted only for request paths starting with `/hubs`.
- Auth refresh cookie name is `ezbias_refresh_token`, path is `/api/auth`, and cookie expiry is 14 days in `AuthController`.
- Auction winner payment deadline is 24 hours from scheduler close time.
- Bid increment is `1,000` VND above the current highest bid.
- Dispute creation window is 3 days from `Order.DeliveredAt`.
- Product multipart create requires 1-8 images; product update accepts at most 8 newly uploaded images.

## Production Must-Change Keys
- `ConnectionStrings__DefaultConnection` or database credentials.
- `Jwt__SecretKey`: use a long random secret; never use the checked-in placeholder or local example value.
- `Jwt__Issuer` and `Jwt__Audience` if production clients expect different values.
- `SePay__ApiToken`, `SePay__AccountNumber`, and `SePay__WebhookSecret`.
- `Brevo__ApiKey` and `Brevo__FromEmail` for real OTP email delivery.
- `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`, and folder if uploads are enabled.
- `Swagger__Enabled=false` unless public Swagger is intended.
- `Debug__ResetSecret` if any debug reset endpoint is enabled in the target environment.
- Frontend origin changes require code change, not config change.
