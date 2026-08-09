namespace Sample.ClaimsPortal.Platform;

/// <summary>
/// A whole-cent monetary amount. Kept as a struct so claim math never allocates.
/// </summary>
public readonly record struct Money(decimal Amount)
{
    public static Money Zero => new(0m);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator *(Money left, decimal factor) => new(left.Amount * factor);

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public override string ToString() =>
        "$" + Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
}
