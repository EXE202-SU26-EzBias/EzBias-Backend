using System.Net;
using System.Net.Mail;
using EzBias.Application.Features.Auth.Services;
using Microsoft.Extensions.Options;

namespace EzBias.API.Integrations;

public sealed class SmtpAuthEmailSender : IAuthEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpAuthEmailSender> _logger;

    public SmtpAuthEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpAuthEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendPasswordResetOtpAsync(string email, string code, DateTimeOffset expiresAt, CancellationToken ct)
        => SendOtpAsync(
            email,
            "Reset your EzBias password",
            $"Your EzBias password reset code is {code}. It expires at {expiresAt:O}.",
            "Password reset",
            code,
            expiresAt,
            ct);

    public Task SendEmailVerificationOtpAsync(string email, string code, DateTimeOffset expiresAt, CancellationToken ct)
        => SendOtpAsync(
            email,
            "Verify your EzBias email",
            $"Your EzBias email verification code is {code}. It expires at {expiresAt:O}.",
            "Email verification",
            code,
            expiresAt,
            ct);

    private async Task SendOtpAsync(
        string email,
        string subject,
        string body,
        string purpose,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "SMTP is not configured. {Purpose} OTP for {Email}: {Code}. ExpiresAt: {ExpiresAt}",
                purpose,
                email,
                code,
                expiresAt);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(email);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        await client.SendMailAsync(message, ct);
    }
}
