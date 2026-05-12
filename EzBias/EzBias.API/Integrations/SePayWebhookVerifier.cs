using System.Security.Cryptography;
using System.Text;
using EzBias.Application.Features.Payments;
using Microsoft.Extensions.Options;

namespace EzBias.API.Integrations;

public class SePayWebhookVerifier : ISePayWebhookVerifier
{
    private readonly SePayOptions _options;

    public SePayWebhookVerifier(IOptions<SePayOptions> options)
    {
        _options = options.Value;
    }

    public bool Verify(string rawBody, string? signature)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret)) return true;
        if (string.IsNullOrWhiteSpace(signature)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        var incoming = signature.Trim().ToLowerInvariant();
        if (incoming.StartsWith("sha256=")) incoming = incoming[7..];

        var computedBytes = Encoding.UTF8.GetBytes(computed);
        var incomingBytes = Encoding.UTF8.GetBytes(incoming);

        return computedBytes.Length == incomingBytes.Length &&
               CryptographicOperations.FixedTimeEquals(computedBytes, incomingBytes);
    }
}
