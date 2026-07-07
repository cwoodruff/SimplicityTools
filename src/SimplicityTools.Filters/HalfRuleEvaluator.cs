using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

/// <summary>
/// Evaluates whether abstraction and dependency growth remain proportionate to project value.
/// </summary>
public static class HalfRuleEvaluator
{
    /// <summary>
    /// Evaluates the Half-Rule against a collected snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to score.</param>
    /// <returns>The filter verdict for the snapshot.</returns>
    public static FilterVerdict Evaluate(SimplicitySnapshot snapshot) =>
        Evaluate(snapshot, FilterThresholds.Default);

    /// <summary>
    /// Evaluates the Half-Rule against a collected snapshot with custom thresholds.
    /// </summary>
    /// <param name="snapshot">The snapshot to score.</param>
    /// <param name="thresholds">The thresholds to score against.</param>
    /// <returns>The filter verdict for the snapshot.</returns>
    public static FilterVerdict Evaluate(SimplicitySnapshot snapshot, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        if (!FilterScoring.HasMeasurableCode(snapshot))
        {
            return FilterScoring.CreateEmptySnapshotVerdict(FilterName.HalfRule);
        }

        var subScores = new[]
        {
            new FilterSubScore("Premature abstraction", FilterScoring.PrematureAbstraction(snapshot.PrematureAbstractionRatio, thresholds.PrematureAbstractionRatioTarget)),
            new FilterSubScore("Dependency accumulation", FilterScoring.UnusedDependencyAccumulation(snapshot.UnusedDependencyCount)),
            new FilterSubScore("Dependency sprawl", FilterScoring.RatioThreshold(snapshot.ExternalDependencyCount, snapshot.TotalProjects, 8.0))
        };

        return FilterScoring.CreateVerdict(
            FilterName.HalfRule,
            thresholds.PassingScore,
            subScores,
            new Dictionary<string, string>
            {
                ["Premature abstraction"] = "Single-implementation interfaces exceed the Half-Rule tolerance.",
                ["Dependency accumulation"] = "Unused dependencies are accumulating faster than the Half-Rule allows.",
                ["Dependency sprawl"] = "External dependency count per project exceeds the Half-Rule target."
            },
            new Dictionary<string, string>
            {
                ["Premature abstraction"] = "Remove or inline single-implementation interfaces before adding more abstractions.",
                ["Dependency accumulation"] = "Delete unused package references so dependency cost stops compounding.",
                ["Dependency sprawl"] = "Consolidate external packages so each project depends on fewer libraries."
            });
    }
}
