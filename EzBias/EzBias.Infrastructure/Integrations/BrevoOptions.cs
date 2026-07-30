namespace EzBias.Infrastructure.Integrations;

public sealed class BrevoOptions
{
    public const string SectionName = "Brevo";

    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "EzBias";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(FromEmail);
}
