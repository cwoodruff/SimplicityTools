using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Payments;

public sealed class InMemoryPayoutLedger : IPayoutLedger
{
    private readonly Dictionary<string, Money> _postings = new(StringComparer.OrdinalIgnoreCase);

    public void Post(string claimNumber, Money amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimNumber);

        _postings.TryGetValue(claimNumber, out var current);
        _postings[claimNumber] = current + amount;
    }

    public Money TotalPostedFor(string claimNumber) =>
        _postings.TryGetValue(claimNumber, out var total) ? total : Money.Zero;
}
