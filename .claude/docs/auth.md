# Auth

## JWT Configuration
- Config section: `Jwt`.
- Defaults in `appsettings.json`: `Issuer = EzBias`, `Audience = EzBias.Client`, `AccessTokenMinutes = 60`, `RefreshTokenDays = 14`.
- `SecretKey` must be supplied outside default appsettings for real environments.
- Access tokens are signed with HMAC SHA-256.
- Claims emitted: `sub`, `email`, `username`, `role`.
- JWT bearer validation checks issuer, audience, lifetime, and signing key.

## Refresh Tokens
- Refresh tokens are 64 random bytes encoded as Base64.
- Stored value is SHA256 hex hash, not the raw token.
- Refresh flow revokes the used token and creates a new refresh token row.
- Logout revokes the presented refresh token if found.
- Refresh token entity tracks `IsRevoked`, `ExpiresAt`, `CreatedAt`, and `RevokedAt`.

## Refresh Cookie
- Cookie name: `ezbias_refresh_token`.
- `HttpOnly = true`.
- `Path = /api/auth`.
- Development: `Secure = false`, `SameSite = Lax`.
- Non-development: `Secure = true`, `SameSite = None`.
- Cookie expiry is set to 14 days in `AuthController.SetRefreshCookie`.
- Refresh endpoint accepts cookie first, request body token second.

## Password Hashing
- User passwords are hashed with `BCrypt.Net.BCrypt.HashPassword`.
- Password verification uses `BCrypt.Net.BCrypt.Verify`.
- Minimum password length enforced by auth service is 6 characters.

## OTP Verification
- OTP expiry is 10 minutes.
- OTP code is a random 6-digit string.
- OTP codes are BCrypt-hashed in `OtpVerification.CodeHash`.
- Creating a new OTP revokes active OTPs for the same user and purpose.
- Purposes implemented in auth flow: `EmailVerification` and `PasswordReset`.
- Register creates a user then sends email verification OTP.
- Login rejects users whose `EmailVerifiedAt` is null.
- Password reset validates active password-reset OTP, marks it used, and replaces password hash.
- Email verification validates active email OTP, marks it used, sets `EmailVerifiedAt`, and queues `UserVerified`.

## Roles and Authorization
- Roles are `User` and `Admin`.
- New registered users get `UserRole.User`.
- Admin-only controllers/actions use `[Authorize(Roles = "Admin")]`, including admin dashboards/users/deposits, payouts, payment manual confirm, and dispute admin actions.
- Most user workflows use `[Authorize]` plus service-level ownership checks.
- Public endpoints include auth registration/login/refresh/OTP flows, public auction reads, catalog-style reads, and the SePay webhook.

## SignalR Auth
- Hub JWT token may be passed as `?access_token=...`.
- Query-string token extraction is limited to paths starting with `/hubs`.
- `AuctionHub` is public; `NotificationHub`, `ChatHub`, and `CallHub` require auth.
