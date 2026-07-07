using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

/// <summary>
/// Evaluates whether the codebase stays understandable, diagnosable, and fixable under pressure.
/// </summary>
public static class TwoAmTestEvaluator
{
    /// <summary>
    /// Evaluates the 2 AM Test against a collected snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to score.</param>
    /// <returns>The filter verdict for the snapshot.</returns>
    public static FilterVerdict Evaluate(SimplicitySnapshot snapshot)
    {
        var subScores = new List<FilterSubScore>
        {
            new("Discoverability", FilterScoring.InverseThreshold(snapshot.PrimaryPathFileCount, 5.0)),
            new("Diagnosability", FilterScoring.InverseThreshold(snapshot.AverageMethodComplexity, 5.0)),
            new("Fixability", FilterScoring.RatioThreshold(snapshot.AbstractionLayerCount, snapshot.TotalProjects, 3.0))
        };

        // An uncomputed onboarding time cannot be scored; the verdict averages the measured
        // sub-scores instead of treating the metric as a perfect zero.
        if (snapshot.EstimatedOnboardingTime is { } onboardingTime)
        {
            subScores.Add(new FilterSubScore("Cognitive load", FilterScoring.InverseThreshold(onboardingTime.TotalHours, 40.0)));
        }

        return FilterScoring.CreateVerdict(
            FilterName.TwoAmTest,
            subScores,
            new Dictionary<string, string>
            {
                ["Discoverability"] = "Primary-path navigation exceeds the 2 AM target of five files.",
                ["Diagnosability"] = "Average method complexity is above the 2 AM target of five.",
                ["Fixability"] = "Abstraction layers per project make fixes harder than the 2 AM target.",
                ["Cognitive load"] = "Estimated onboarding time is above the 2 AM target of 40 hours."
            },
            new Dictionary<string, string>
            {
                ["Discoverability"] = "Reduce the number of files on the primary path so the main flow is easier to trace.",
                ["Diagnosability"] = "Break up complex methods until average complexity trends back toward five or lower.",
                ["Fixability"] = "Collapse unnecessary abstraction layers so each project has fewer moving parts to change.",
                ["Cognitive load"] = "Trim indirection and spread so onboarding can stay within roughly one work week."
            });
    }
}
