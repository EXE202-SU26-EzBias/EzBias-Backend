namespace EzBias.Infrastructure.Integrations;

public class SePayOptions
{
    public const string SectionName = "SePay";
    public string BaseUrl { get; set; } = "https://my.sepay.vn";
    public string ApiToken { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public int DefaultLimit { get; set; } = 200;
}
