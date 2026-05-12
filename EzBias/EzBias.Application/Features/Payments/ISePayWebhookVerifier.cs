namespace EzBias.Application.Features.Payments;

public interface ISePayWebhookVerifier
{
    bool Verify(string rawBody, string? signature);
}
