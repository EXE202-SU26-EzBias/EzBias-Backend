namespace EzBias.Application.Features.Deposits;

public interface IDepositPolicy
{
    /// <summary>The fraction of the floor price required as a bid deposit (e.g. 0.10 = 10%).</summary>
    decimal DepositFractionOfFloor { get; }

    /// <summary>
    /// Computes the required bid deposit for an auction as <c>floorPrice * DepositFractionOfFloor</c>,
    /// rounded to a whole number of VND. Returns 0 for a non-positive floor price (no deposit gate).
    /// </summary>
    decimal ComputeRequiredDeposit(decimal floorPrice);
}
