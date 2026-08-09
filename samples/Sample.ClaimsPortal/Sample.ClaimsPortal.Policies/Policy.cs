using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Policies;

public sealed record Policy(
    string PolicyNumber,
    string HolderName,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    IReadOnlyList<Coverage> Coverages)
{
    public bool IsActiveOn(DateOnly date) => date >= EffectiveFrom && date <= EffectiveTo;

    public Coverage? CoverageFor(ClaimCategory category) =>
        Coverages.FirstOrDefault(coverage => coverage.Category == category);
}
