namespace EzBias.Infrastructure.Integrations;

public sealed class DepositOptions
{
    public const string SectionName = "Deposit";

    /// <summary>Fraction of the floor price taken as the required bid deposit (0.10 = 10%).</summary>
    public decimal DepositFractionOfFloor { get; set; } = 0.10m;
}
