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
    public static FilterVerdict Evaluate(SimplicitySnapshot snapshot) =>
        Evaluate(snapshot, FilterThresholds.Default);

    /// <summary>
    /// Evaluates the 2 AM Test against a collected snapshot with custom thresholds.
    /// </summary>
    /// <param name="snapshot">The snapshot to score.</param>
    /// <param name="thresholds">The thresholds to score against.</param>
    /// <returns>The filter verdict for the snapshot.</returns>
    public static FilterVerdict Evaluate(SimplicitySnapshot snapshot, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        if (!FilterScoring.HasMeasurableCode(snapshot))
        {
            return FilterScoring.CreateEmptySnapshotVerdict(FilterName.TwoAmTest);
        }

        // Discoverability measures primary-path files per project — “can one flow be traced
        // through at most ~5 files at 2 AM.” An absolute file cap would penalize exactly the
        // concentration PrimaryPathFirst rewards, making the two filters mathematically
        // incompatible for any solution above ~8 files (issue #90).
        var primaryPathFilesPerProject = (double)snapshot.PrimaryPathFileCount / Math.Max(1, snapshot.TotalProjects);

        var subScores = new List<FilterSubScore>
        {
            new("Discoverability", FilterScoring.InverseThreshold(primaryPathFilesPerProject, 5.0)),
            new("Diagnosability", FilterScoring.InverseThreshold(snapshot.AverageMethodComplexity, thresholds.MaxMethodComplexity)),
            new("Fixability", FilterScoring.RatioThreshold(snapshot.AbstractionLayerCount, snapshot.TotalProjects, 3.0))
        };

        // An uncomputed onboarding time cannot be scored; the verdict averages the measured
        // sub-scores instead of treating the metric as a perfect zero.
        if (snapshot.EstimatedOnboardingTime is { } onboardingTime)
        {
            subScores.Add(new FilterSubScore("Cognitive load", FilterScoring.InverseThreshold(onboardingTime.TotalHours, thresholds.MaxOnboardingHours)));
        }

        return FilterScoring.CreateVerdict(
            FilterName.TwoAmTest,
            thresholds.PassingScore,
            subScores,
            new Dictionary<string, string>
            {
                ["Discoverability"] = "Primary-path navigation exceeds the 2 AM target of five files per project.",
                ["Diagnosability"] = "Average method complexity is above the 2 AM target of five.",
                ["Fixability"] = "Abstraction layers per project make fixes harder than the 2 AM target.",
                ["Cognitive load"] = "Estimated onboarding time is above the 2 AM target of 40 hours."
            },
            new Dictionary<string, string>
            {
                ["Discoverability"] = "Reduce the primary-path files carried by each project so one flow stays traceable.",
                ["Diagnosability"] = "Break up complex methods until average complexity trends back toward five or lower.",
                ["Fixability"] = "Collapse unnecessary abstraction layers so each project has fewer moving parts to change.",
                ["Cognitive load"] = "Trim indirection and spread so onboarding can stay within roughly one work week."
            });
    }
}
