using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

/// <summary>
/// Severity attached to a <see cref="ScoreBand" />, for renderers to map onto styling.
/// </summary>
public enum ScoreBandSeverity
{
    /// <summary>The metric is at or better than its configured target.</summary>
    Good,

    /// <summary>The metric is past its configured target but not yet critical.</summary>
    Warn,

    /// <summary>The metric is far past its configured target.</summary>
    Critical
}

/// <summary>
/// A human-readable verdict band for a metric value.
/// </summary>
/// <param name="Label">The band label (for example, "Good").</param>
/// <param name="Severity">The severity renderers use to style the band.</param>
public readonly record struct ScoreBand(string Label, ScoreBandSeverity Severity);

/// <summary>
/// The single scoring model shared by every output surface (terminal budget, HTML report,
/// simplicity score). All penalties and bands derive from <see cref="FilterThresholds" />, so
/// configuring <c>simplicity.json</c> moves every verdict together instead of the surfaces
/// disagreeing. With default thresholds the values are identical to the historical formulas.
/// </summary>
public static class SimplicityScoring
{
    /// <summary>
    /// Computes the 0–100 simplicity score. Penalty anchors are expressed as multiples of the
    /// configured thresholds; at <see cref="FilterThresholds.Default" /> they reproduce the
    /// historical formula exactly (complexity penalty starting at 3 = 0.6×5, primary-path
    /// penalty vanishing at 0.8 = 4/3×0.60, full premature-abstraction penalty at 1.0 = 4×0.25).
    /// </summary>
    public static double CalculateScore(SimplicitySnapshot snapshot, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(thresholds);

        var score = 100.0;

        var prematureAnchor = 4.0 * thresholds.PrematureAbstractionRatioTarget;
        score -= Math.Clamp(snapshot.PrematureAbstractionRatio / prematureAnchor, 0.0, 1.0) * 30;

        score -= Math.Min(snapshot.UnusedDependencyCount * 3, 20);

        var complexityOnset = 0.6 * thresholds.MaxMethodComplexity;
        var complexityRange = 2.0 * thresholds.MaxMethodComplexity;
        score -= Math.Clamp((snapshot.AverageMethodComplexity - complexityOnset) / complexityRange, 0.0, 1.0) * 20;

        var primaryPathAnchor = 4.0 / 3.0 * thresholds.PrimaryPathRatioTarget;
        score -= Math.Clamp((primaryPathAnchor - snapshot.PrimaryPathRatio) / primaryPathAnchor, 0.0, 1.0) * 30;

        return Math.Clamp(score, 0.0, 100.0);
    }

    /// <summary>
    /// Bands an average method complexity value against the configured target:
    /// Excellent under 0.6×target, Good under the target, Moderate under 2×target, High above.
    /// </summary>
    public static ScoreBand GetComplexityBand(double averageMethodComplexity, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        var target = thresholds.MaxMethodComplexity;
        return averageMethodComplexity switch
        {
            _ when averageMethodComplexity < 0.6 * target => new ScoreBand("Excellent", ScoreBandSeverity.Good),
            _ when averageMethodComplexity < target => new ScoreBand("Good", ScoreBandSeverity.Good),
            _ when averageMethodComplexity < 2.0 * target => new ScoreBand("Moderate", ScoreBandSeverity.Warn),
            _ => new ScoreBand("High", ScoreBandSeverity.Critical)
        };
    }

    /// <summary>
    /// Bands a primary-path ratio against the configured target:
    /// Good above 4/3×target, Review above 5/6×target, Critical below.
    /// </summary>
    public static ScoreBand GetPrimaryPathBand(double primaryPathRatio, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        var target = thresholds.PrimaryPathRatioTarget;
        return primaryPathRatio switch
        {
            _ when primaryPathRatio > 4.0 / 3.0 * target => new ScoreBand("Good", ScoreBandSeverity.Good),
            _ when primaryPathRatio > 5.0 / 6.0 * target => new ScoreBand("Review", ScoreBandSeverity.Warn),
            _ => new ScoreBand("Critical", ScoreBandSeverity.Critical)
        };
    }

    /// <summary>
    /// Bands a premature-abstraction ratio against the configured target:
    /// Good under 1.2×target, Review under 2.4×target, Critical above.
    /// </summary>
    public static ScoreBand GetPrematureAbstractionBand(double prematureAbstractionRatio, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        var target = thresholds.PrematureAbstractionRatioTarget;
        return prematureAbstractionRatio switch
        {
            _ when prematureAbstractionRatio < 1.2 * target => new ScoreBand("Good", ScoreBandSeverity.Good),
            _ when prematureAbstractionRatio < 2.4 * target => new ScoreBand("Review", ScoreBandSeverity.Warn),
            _ => new ScoreBand("Critical", ScoreBandSeverity.Critical)
        };
    }

    /// <summary>
    /// Bands an onboarding-hours value against the configured target:
    /// Efficient under 0.4×target, Moderate under the target, Substantial above.
    /// </summary>
    public static ScoreBand GetOnboardingBand(double onboardingHours, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        var target = thresholds.MaxOnboardingHours;
        return onboardingHours switch
        {
            _ when onboardingHours < 0.4 * target => new ScoreBand("Efficient", ScoreBandSeverity.Good),
            _ when onboardingHours < target => new ScoreBand("Moderate", ScoreBandSeverity.Warn),
            _ => new ScoreBand("Substantial", ScoreBandSeverity.Critical)
        };
    }
}
