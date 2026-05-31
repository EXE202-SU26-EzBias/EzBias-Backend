using EzBias.Application.Features.Deposits;
using Microsoft.Extensions.Options;

namespace EzBias.API.Integrations;

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
        // No deposit gate for a non-positive floor price.
        if (floorPrice <= 0m)
        {
            return 0m;
        }

        // Required deposit = floor * fraction, rounded to a whole number of VND.
        var raw = floorPrice * DepositFractionOfFloor;
        return Math.Round(raw, 0, MidpointRounding.AwayFromZero);
    }
}
