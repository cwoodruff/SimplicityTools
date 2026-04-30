using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

public static class HalfRuleEvaluator
{
    public static FilterVerdict Evaluate(SimplicitySnapshot snapshot)
    {
        var subScores = new[]
        {
            new FilterSubScore("Premature abstraction", FilterScoring.PrematureAbstraction(snapshot.PrematureAbstractionRatio)),
            new FilterSubScore("Dependency accumulation", FilterScoring.UnusedDependencyAccumulation(snapshot.UnusedDependencyCount)),
            new FilterSubScore("Dependency sprawl", FilterScoring.RatioThreshold(snapshot.ExternalDependencyCount, snapshot.TotalProjects, 8.0))
        };

        return FilterScoring.CreateVerdict(
            FilterName.HalfRule,
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
