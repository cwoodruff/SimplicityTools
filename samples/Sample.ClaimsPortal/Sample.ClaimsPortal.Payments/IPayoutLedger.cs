using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Payments;

/// <summary>
/// SF0001 hit: one implementation, <see cref="InMemoryPayoutLedger" />.
/// </summary>
public interface IPayoutLedger
{
    void Post(string claimNumber, Money amount);

    Money TotalPostedFor(string claimNumber);
}
