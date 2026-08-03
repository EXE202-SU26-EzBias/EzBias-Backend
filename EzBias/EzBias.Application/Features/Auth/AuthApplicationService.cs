using EzBias.Application.Common.Results;
using EzBias.Application.Features.Auth.Dtos;
using EzBias.Application.Features.Auth.Services;
using EzBias.Application.Features.Notifications;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using System.Security.Cryptography;

namespace EzBias.Application.Features.Auth;

public class AuthApplicationService : IAuthApplicationService
{
    private const int OtpExpiryMinutes = 10;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IOtpVerificationRepository _otpVerifications;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuthEmailSender _emailSender;

    public AuthApplicationService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IOtpVerificationRepository otpVerifications,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAuthEmailSender emailSender)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _otpVerifications = otpVerifications;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailSender = emailSender;
    }

    public async Task<Result<AuthResult>> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return Result<AuthResult>.Fail("Password must be at least 6 chars.", ApplicationErrorCode.Validation);

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var normalizedUsername = req.Username.Trim().ToLowerInvariant();

        var exists = await _users.ExistsByEmailOrUsernameAsync(normalizedEmail, normalizedUsername, ct);
        if (exists)
            return Result<AuthResult>.Fail("Email or username already exists.", ApplicationErrorCode.Conflict);

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Username = req.Username.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(req.Password),
            Role = UserRole.User,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _users.Add(user);
        await _uow.SaveChangesAsync(ct);

        await CreateAndSendOtpAsync(user, OtpPurpose.EmailVerification, ct);

        var auth = await BuildAuthResponseAsync(user, ct);
        return Result<AuthResult>.Ok(auth);
    }

    public async Task<Result<AuthResult>> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var key = req.EmailOrUsername.Trim().ToLowerInvariant();
        var user = await _users.GetByLoginKeyAsync(key, ct);

        if (user is null || user.DeletedAt != null)
            return Result<AuthResult>.Fail("Invalid credentials.", ApplicationErrorCode.Unauthorized);

        if (!_passwordHasher.Verify(req.Password, user.PasswordHash))
            return Result<AuthResult>.Fail("Invalid credentials.", ApplicationErrorCode.Unauthorized);

        if (user.EmailVerifiedAt is null)
            return Result<AuthResult>.Fail("Email is not verified.", ApplicationErrorCode.Unauthorized);

        var auth = await BuildAuthResponseAsync(user, ct);
        return Result<AuthResult>.Ok(auth);
    }

    public async Task<Result<AuthResult>> RefreshAsync(RefreshRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return Result<AuthResult>.Fail("Refresh token is required.", ApplicationErrorCode.Validation);

        var hash = _tokenService.HashRefreshToken(req.RefreshToken!);
        var stored = await _refreshTokens.GetByHashWithUserAsync(hash, ct);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result<AuthResult>.Fail("Invalid refresh token.", ApplicationErrorCode.Unauthorized);

        stored.IsRevoked = true;
        stored.RevokedAt = DateTimeOffset.UtcNow;

        var user = stored.User;
        var access = _tokenService.CreateAccessToken(user);
        var refresh = _tokenService.CreateRefreshToken();

        _refreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refresh),
            ExpiresAt = _tokenService.GetRefreshTokenExpiry(),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(ct);

        return Result<AuthResult>.Ok(new AuthResult(
            access,
            _tokenService.AccessTokenExpiresInSeconds,
            refresh,
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString()));
    }

    public async Task LogoutAsync(RefreshRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return;

        var hash = _tokenService.HashRefreshToken(req.RefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, ct);

        if (stored is null)
            return;

        stored.IsRevoked = true;
        stored.RevokedAt = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(req.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return Result.Fail("Email is required.", ApplicationErrorCode.Validation);

        var user = await _users.GetByEmailAsync(normalizedEmail, ct);
        if (user is null || user.DeletedAt != null)
            return Result.Ok();

        await CreateAndSendOtpAsync(user, OtpPurpose.PasswordReset, ct);
        return Result.Ok();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return Result.Fail("Password must be at least 6 chars.", ApplicationErrorCode.Validation);

        var normalizedEmail = NormalizeEmail(req.Email);
        var user = await _users.GetByEmailAsync(normalizedEmail, ct);
        if (user is null || user.DeletedAt != null)
            return Result.Fail("Invalid or expired code.", ApplicationErrorCode.Validation);

        var now = DateTimeOffset.UtcNow;
        var otp = await FindMatchingOtpAsync(user.Id, OtpPurpose.PasswordReset, req.Code, now, ct);
        if (otp is null)
            return Result.Fail("Invalid or expired code.", ApplicationErrorCode.Validation);

        otp.IsUsed = true;
        user.PasswordHash = _passwordHasher.Hash(req.NewPassword);
        user.UpdatedAt = now;

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RequestEmailVerificationAsync(RequestEmailVerificationRequest req, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(req.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return Result.Fail("Email is required.", ApplicationErrorCode.Validation);

        var user = await _users.GetByEmailAsync(normalizedEmail, ct);
        if (user is null || user.DeletedAt != null || user.EmailVerifiedAt != null)
            return Result.Ok();

        await CreateAndSendOtpAsync(user, OtpPurpose.EmailVerification, ct);
        return Result.Ok();
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest req, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(req.Email);
        var user = await _users.GetByEmailAsync(normalizedEmail, ct);
        if (user is null || user.DeletedAt != null)
            return Result.Fail("Invalid or expired code.", ApplicationErrorCode.Validation);

        var now = DateTimeOffset.UtcNow;
        var otp = await FindMatchingOtpAsync(user.Id, OtpPurpose.EmailVerification, req.Code, now, ct);
        if (otp is null)
            return Result.Fail("Invalid or expired code.", ApplicationErrorCode.Validation);

        otp.IsUsed = true;
        user.EmailVerifiedAt ??= now;
        user.UpdatedAt = now;

        _notifications.Add(_notificationFactory.UserVerified(user.Id));

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<MeResponse>> MeAsync(long userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<MeResponse>.Fail("Unauthorized.", ApplicationErrorCode.Unauthorized);

        return Result<MeResponse>.Ok(new MeResponse(user.Id, user.Username, user.Email, user.FullName, user.Role.ToString()));
    }

    private async Task<AuthResult> BuildAuthResponseAsync(User user, CancellationToken ct)
    {
        var access = _tokenService.CreateAccessToken(user);
        var refresh = _tokenService.CreateRefreshToken();

        _refreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refresh),
            ExpiresAt = _tokenService.GetRefreshTokenExpiry(),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(ct);

        return new AuthResult(
            access,
            _tokenService.AccessTokenExpiresInSeconds,
            refresh,
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString());
    }

    private async Task CreateAndSendOtpAsync(User user, OtpPurpose purpose, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var code = GenerateOtp();
        var expiresAt = now.AddMinutes(OtpExpiryMinutes);

        await _otpVerifications.RevokeActiveAsync(user.Id, purpose, now, ct);
        _otpVerifications.Add(new OtpVerification
        {
            UserId = user.Id,
            Channel = OtpChannel.Email,
            Purpose = purpose,
            CodeHash = _passwordHasher.Hash(code),
            ExpiresAt = expiresAt,
            CreatedAt = now
        });

        await _uow.SaveChangesAsync(ct);

        if (purpose == OtpPurpose.PasswordReset)
            await _emailSender.SendPasswordResetOtpAsync(user.Email, code, expiresAt, ct);
        else if (purpose == OtpPurpose.EmailVerification)
            await _emailSender.SendEmailVerificationOtpAsync(user.Email, code, expiresAt, ct);
    }

    private static string GenerateOtp()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private async Task<OtpVerification?> FindMatchingOtpAsync(
        long userId,
        OtpPurpose purpose,
        string code,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var normalizedCode = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return null;

        var activeOtps = await _otpVerifications.GetActiveAsync(userId, purpose, now, ct);
        return activeOtps.FirstOrDefault(otp => _passwordHasher.Verify(normalizedCode, otp.CodeHash));
    }

    private static string NormalizeEmail(string email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}
