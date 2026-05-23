using EzBias.Application.Features.Payments;
using Microsoft.Extensions.Options;

namespace EzBias.API.Integrations;

public sealed class ConfiguredCommissionRateProvider : ICommissionRateProvider
{
    private readonly CommissionOptions _options;

    public ConfiguredCommissionRateProvider(IOptions<CommissionOptions> options)
    {
        _options = options.Value;
    }

    public decimal GetRatePercent()
        => Math.Clamp(_options.RatePercent, 5m, 10m);
}
