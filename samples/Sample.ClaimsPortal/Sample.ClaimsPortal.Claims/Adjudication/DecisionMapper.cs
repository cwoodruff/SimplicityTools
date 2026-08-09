using Sample.ClaimsPortal.Claims.Journal;

namespace Sample.ClaimsPortal.Claims.Adjudication;

public sealed class DecisionMapper
{
    private readonly DecisionStore _store;

    public DecisionMapper(DecisionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public ClaimDecision Map(ClaimDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        var normalized = decision with { Reason = decision.Reason.Trim() };
        return _store.Save(normalized);
    }
}
