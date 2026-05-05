using EzBias.Application.Features.Auth.Dtos;
using EzBias.Application.Features.Auth.Services;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Auth;

public class AuthApplicationService : IAuthApplicationService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthApplicationService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<(bool Success, string? Error, AuthResult? Data)> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return (false, "Password must be at least 6 chars.", null);

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var normalizedUsername = req.Username.Trim().ToLowerInvariant();

        var exists = await _users.ExistsByEmailOrUsernameAsync(normalizedEmail, normalizedUsername, ct);
        if (exists)
            return (false, "Email or username already exists.", null);

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

        var auth = await BuildAuthResponseAsync(user, ct);
        return (true, null, auth);
    }

    public async Task<(bool Success, string? Error, AuthResult? Data)> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var key = req.EmailOrUsername.Trim().ToLowerInvariant();
        var user = await _users.GetByLoginKeyAsync(key, ct);

        if (user is null || user.DeletedAt != null)
            return (false, "Invalid credentials.", null);

        if (!_passwordHasher.Verify(req.Password, user.PasswordHash))
            return (false, "Invalid credentials.", null);

        var auth = await BuildAuthResponseAsync(user, ct);
        return (true, null, auth);
    }

    public async Task<(bool Success, string? Error, AuthResult? Data)> RefreshAsync(RefreshRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return (false, "Refresh token is required.", null);

        var hash = _tokenService.HashRefreshToken(req.RefreshToken!);
        var stored = await _refreshTokens.GetByHashWithUserAsync(hash, ct);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return (false, "Invalid refresh token.", null);

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

        return (true, null, new AuthResult(
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

    public async Task<(bool Success, MeResponse? Data)> MeAsync(long userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return (false, null);

        return (true, new MeResponse(user.Id, user.Username, user.Email, user.FullName, user.Role.ToString()));
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
}
