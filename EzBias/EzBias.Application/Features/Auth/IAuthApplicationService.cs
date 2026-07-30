using EzBias.Application.Common.Results;
using EzBias.Application.Features.Auth.Dtos;

namespace EzBias.Application.Features.Auth;

public interface IAuthApplicationService
{
    Task<Result<AuthResult>> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<Result<AuthResult>> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<Result<AuthResult>> RefreshAsync(RefreshRequest req, CancellationToken ct);
    Task LogoutAsync(RefreshRequest req, CancellationToken ct);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct);
    Task<Result> RequestEmailVerificationAsync(RequestEmailVerificationRequest req, CancellationToken ct);
    Task<Result> VerifyEmailAsync(VerifyEmailRequest req, CancellationToken ct);
    Task<Result<MeResponse>> MeAsync(long userId, CancellationToken ct);
}
