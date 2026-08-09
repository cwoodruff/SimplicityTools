namespace Sample.ClaimsPortal.Fraud;

public sealed class VelocitySignal : IFraudSignal
{
    public string Name => "velocity";

    public FraudScore Evaluate(FraudContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ClaimsInLastNinetyDays switch
        {
            <= 1 => FraudScore.None,
            2 => new FraudScore(15),
            3 => new FraudScore(35),
            _ => new FraudScore(55)
        };
    }
}
