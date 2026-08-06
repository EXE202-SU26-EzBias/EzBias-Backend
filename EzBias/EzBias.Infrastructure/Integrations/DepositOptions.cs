namespace EzBias.Infrastructure.Integrations;

public sealed class DepositOptions
{
    public const string SectionName = "Deposit";

    public decimal DepositFractionOfFloor { get; set; } = 0.10m;
}
