namespace Sample.ClaimsPortal.Fraud;

public readonly record struct FraudScore(int Value)
{
    public static FraudScore None => new(0);

    public bool RequiresManualReview => Value >= 60;

    public static FraudScore operator +(FraudScore left, FraudScore right) =>
        new(Math.Clamp(left.Value + right.Value, 0, 100));
}
