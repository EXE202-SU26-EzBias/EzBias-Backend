namespace EzBias.API.Integrations;

public sealed class CommissionOptions
{
    public const string SectionName = "Commission";

    public decimal RatePercent { get; set; } = 8m;
}
