namespace Sample.ClaimsPortal.Platform.Identity;

public sealed class SequentialClaimNumberGenerator : IClaimNumberGenerator
{
    private int _sequence;

    public string Next(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        var value = Interlocked.Increment(ref _sequence);
        return $"{prefix}-{value:D6}";
    }
}
