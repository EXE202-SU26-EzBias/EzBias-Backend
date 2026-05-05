using EzBias.Domain.Entities;

namespace EzBias.Application.Features.Auth.Services;

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTimeOffset GetRefreshTokenExpiry();
    int AccessTokenExpiresInSeconds { get; }
}
