# Auth

## JWT Configuration
- Config section: `Jwt`.
- Defaults in `appsettings.json`: `Issuer = EzBias`, `Audience = EzBias.Client`, `AccessTokenMinutes = 60`, `RefreshTokenDays = 14`.
- `SecretKey` must be supplied outside default appsettings for real environments.
- Access tokens are signed with HMAC SHA-256.
- Claims emitted: `sub`, `email`, `username`, `role`.
- JWT bearer validation checks issuer, audience, lifetime, and signing key.
- `NameClaimType = "sub"` and `RoleClaimType = "role"` are configured.

## Registration and Login
- `POST /api/auth/register` requires a password of at least 6 characters and unique normalized email/username.
- Registration creates a `UserRole.User`, saves the user, creates and sends an email verification OTP, creates access/refresh tokens, and sets the refresh cookie.
- Current registration returns tokens before email verification is completed.
- `POST /api/auth/login` accepts email or username, rejects deleted users, verifies the BCrypt password, and rejects users whose `EmailVerifiedAt` is null.
- `GET /api/auth/me` returns ID, username, email, full name, and role for the authenticated principal.
- `api/users/me` returns/updates the wider profile, including address and bank information used by payout/refund workflows.

## Refresh Tokens
- Refresh tokens are 64 random bytes encoded as Base64.
- Stored value is a SHA256 hex hash, not the raw token.
- Refresh flow revokes the used token and creates a new refresh token row.
- Logout revokes the presented refresh token when found.
- Refresh token entity tracks `IsRevoked`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, and optional device info.

## Refresh Cookie
- Cookie name: `ezbias_refresh_token`.
- `HttpOnly = true`.
- `Path = /api/auth`.
- Development: `Secure = false`, `SameSite = Lax`.
- Non-development: `Secure = true`, `SameSite = None`.
- Cookie expiry is hard-coded in `AuthController.SetRefreshCookie` to 14 days.
- Refresh and logout endpoints prefer the cookie token; request body token is fallback.

## OTP Verification
- OTP expiry is 10 minutes.
- OTP code is a random 6-digit string.
- OTP codes are BCrypt-hashed in `OtpVerification.CodeHash`.
- Creating a new OTP revokes active OTPs for the same user and purpose.
- Implemented purposes in auth flow: `EmailVerification` and `PasswordReset`.
- Email verification validates an active email OTP, marks it used, sets `EmailVerifiedAt` when empty, and queues `UserVerified`.
- Forgot password intentionally does not reveal whether the email exists; when a non-deleted user exists, it sends a password-reset OTP.
- Password reset validates an active password-reset OTP, marks it used, and replaces the password hash.
- Brevo sends OTP emails when configured. If Brevo is not configured, the OTP code and expiry are logged.

## User Lifecycle
- Registered users are soft-deletable by admins through `api/admin/users`.
- Admins cannot soft-delete their own account.
- Admin restore clears `DeletedAt`.
- `DELETE /api/users/by-email?email=...` is anonymous and hard-deletes only unverified users. This is a cleanup helper for failed registration/verification flows.
- Login, OTP request/verification, forgot password, and chat counterpart lookup reject deleted users.

## Roles and Authorization
- Roles are `User` and `Admin`.
- New registered users get `UserRole.User`.
- Admin-created users can be assigned either role, but the admin-create flow does not set `EmailVerifiedAt`.
- Admin-only controllers/actions use `[Authorize(Roles = "Admin")]`, including admin dashboards/users/deposits, payouts, payment manual confirm, dispute admin actions, and review moderation.
- Most user workflows use `[Authorize]` plus service-level ownership checks.
- Public endpoints include registration/login/refresh/OTP flows, public auction reads, catalog reads, contact submit, the SePay webhook, selected debug endpoints, and unverified-user cleanup.

## SignalR Auth
- Hub JWT token may be passed as `?access_token=...`.
- Query-string token extraction is limited to paths starting with `/hubs`.
- `AuctionHub` is public; `NotificationHub`, `ChatHub`, and `CallHub` require auth.
