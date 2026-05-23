namespace EzBias.Application.Features.Auth.Services;

public interface IAuthEmailSender
{
    Task SendPasswordResetOtpAsync(string email, string code, DateTimeOffset expiresAt, CancellationToken ct);
    Task SendEmailVerificationOtpAsync(string email, string code, DateTimeOffset expiresAt, CancellationToken ct);
}
