namespace Sample.ClaimsPortal.Claims.Journal;

public sealed class DecisionStore
{
    private readonly DecisionJournal _journal;
    private readonly Dictionary<string, ClaimDecision> _decisions = new(StringComparer.OrdinalIgnoreCase);

    public DecisionStore(DecisionJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);
        _journal = journal;
    }

    public ClaimDecision? Find(string claimNumber) =>
        _decisions.TryGetValue(claimNumber, out var decision) ? decision : null;

    public ClaimDecision Save(ClaimDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        _decisions[decision.ClaimNumber] = decision;
        _journal.Append(decision);
        return decision;
    }
}
