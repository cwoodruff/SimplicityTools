using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Fraud;

/// <summary>
/// Deliberate contrast case: this interface has TWO implementations
/// (<see cref="VelocitySignal" /> and <see cref="AmountAnomalySignal" />), so SF0001 stays quiet.
/// An abstraction that is actually polymorphic is an abstraction that earns its keep.
/// </summary>
public interface IFraudSignal
{
    string Name { get; }

    FraudScore Evaluate(FraudContext context);
}

public sealed record FraudContext(
    string PolicyNumber,
    ClaimCategory Category,
    Money ClaimedAmount,
    int ClaimsInLastNinetyDays,
    Money AverageHistoricalClaim);
