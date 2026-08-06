using EzBias.Application.Features.Deposits;
using Microsoft.Extensions.Options;

namespace EzBias.Infrastructure.Integrations;

public sealed class ConfiguredDepositPolicy : IDepositPolicy
{
    private readonly DepositOptions _options;

    public ConfiguredDepositPolicy(IOptions<DepositOptions> options)
    {
        _options = options.Value;
    }

    public decimal DepositFractionOfFloor => _options.DepositFractionOfFloor;

    public decimal ComputeRequiredDeposit(decimal floorPrice)
    {
        if (floorPrice <= 0m)
        {
            return 0m;
        }

        var raw = floorPrice * DepositFractionOfFloor;
        return Math.Round(raw, 0, MidpointRounding.AwayFromZero);
    }
}
