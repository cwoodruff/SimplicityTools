namespace Sample.ClaimsPortal.Fraud;

public sealed class FraudScreener
{
    private readonly IReadOnlyList<IFraudSignal> _signals;

    public FraudScreener(IReadOnlyList<IFraudSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        _signals = signals;
    }

    public static FraudScreener CreateDefault() => new([new VelocitySignal(), new AmountAnomalySignal()]);

    public FraudScore Screen(FraudContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var total = FraudScore.None;
        foreach (var signal in _signals)
        {
            total += signal.Evaluate(context);
        }

        return total;
    }
}
