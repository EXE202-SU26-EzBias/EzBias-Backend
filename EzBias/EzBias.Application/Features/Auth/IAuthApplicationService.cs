using EzBias.Application.Features.Auth.Dtos;

namespace EzBias.Application.Features.Auth;

public interface IAuthApplicationService
{
    Task<(bool Success, string? Error, AuthResult? Data)> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<(bool Success, string? Error, AuthResult? Data)> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<(bool Success, string? Error, AuthResult? Data)> RefreshAsync(RefreshRequest req, CancellationToken ct);
    Task LogoutAsync(RefreshRequest req, CancellationToken ct);
    Task<(bool Success, MeResponse? Data)> MeAsync(long userId, CancellationToken ct);
}
