using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Fraud;

public sealed class AmountAnomalySignal : IFraudSignal
{
    public string Name => "amount-anomaly";

    public FraudScore Evaluate(FraudContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.AverageHistoricalClaim == Money.Zero)
        {
            return FraudScore.None;
        }

        var ratio = context.ClaimedAmount.Amount / context.AverageHistoricalClaim.Amount;
        return ratio switch
        {
            < 2m => FraudScore.None,
            < 4m => new FraudScore(20),
            < 8m => new FraudScore(40),
            _ => new FraudScore(65)
        };
    }
}
