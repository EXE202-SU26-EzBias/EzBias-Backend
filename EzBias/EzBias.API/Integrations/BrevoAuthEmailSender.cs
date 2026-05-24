using System.Text;
using System.Text.Json;
using EzBias.Application.Features.Auth.Services;
using Microsoft.Extensions.Options;

namespace EzBias.API.Integrations;

public sealed class BrevoAuthEmailSender : IAuthEmailSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly BrevoOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BrevoAuthEmailSender> _logger;

    public BrevoAuthEmailSender(
        IOptions<BrevoOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<BrevoAuthEmailSender> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task SendPasswordResetOtpAsync(string email, string code, DateTimeOffset expiresAt, CancellationToken ct)
        => SendOtpAsync(
            email,
            subject: "Reset your EzBias password",
            body: $"Your EzBias password reset code is {code}. It expires at {expiresAt:O}.",
            purpose: "Password reset",
            code,
            expiresAt,
            ct);

    public Task SendEmailVerificationOtpAsync(string email, string code, DateTimeOffset expiresAt, CancellationToken ct)
        => SendOtpAsync(
            email,
            subject: "Verify your EzBias email",
            body: $"Your EzBias email verification code is {code}. It expires at {expiresAt:O}.",
            purpose: "Email verification",
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
                "Brevo is not configured. {Purpose} OTP for {Email}: {Code}. ExpiresAt: {ExpiresAt}",
                purpose, email, code, expiresAt);
            return;
        }

        var payload = new
        {
            sender = new { name = _options.FromName, email = _options.FromEmail },
            to = new[] { new { email } },
            subject,
            textContent = body
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", _options.ApiKey);

        var client = _httpClientFactory.CreateClient("Brevo");
        using var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Brevo email failed ({StatusCode}). {Purpose} OTP for {Email}: {Code}. ExpiresAt: {ExpiresAt}. Response: {ResponseBody}",
                (int)response.StatusCode, purpose, email, code, expiresAt, responseBody);
        }
    }
}
