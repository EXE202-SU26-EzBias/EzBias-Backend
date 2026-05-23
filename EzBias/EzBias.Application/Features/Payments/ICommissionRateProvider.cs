namespace EzBias.Application.Features.Payments;

public interface ICommissionRateProvider
{
    decimal GetRatePercent();
}
