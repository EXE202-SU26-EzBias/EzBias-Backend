namespace EzBias.Domain.Enums;

/// <summary>
/// Result of asking a domain entity to move to another lifecycle state.
/// The outcome is intentionally not an exception: application services can
/// map invalid transitions to the existing API error contract.
/// </summary>
public enum TransitionOutcome
{
    Applied = 1,
    NoOp = 2,
    Terminal = 3,
    Invalid = 4
}
