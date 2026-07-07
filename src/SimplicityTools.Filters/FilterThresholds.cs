namespace SimplicityTools.Filters;

/// <summary>
/// Tunable thresholds consumed by the filter evaluators. Values map one-to-one onto the
/// <c>filters</c> section of <c>simplicity.json</c>.
/// </summary>
/// <param name="PrimaryPathRatioTarget">The primary-path ratio at which concentration scores 1.0.</param>
/// <param name="PrematureAbstractionRatioTarget">The premature-abstraction ratio above which the sub-score starts failing.</param>
/// <param name="MaxMethodComplexity">The average method complexity treated as the diagnosability target.</param>
/// <param name="MaxOnboardingHours">The onboarding-hours budget treated as the cognitive-load target.</param>
/// <param name="PassingScore">The overall score a filter must reach to pass.</param>
public sealed record FilterThresholds(
    double PrimaryPathRatioTarget,
    double PrematureAbstractionRatioTarget,
    double MaxMethodComplexity,
    double MaxOnboardingHours,
    double PassingScore)
{
    /// <summary>
    /// The built-in defaults, matching the documented out-of-the-box behavior.
    /// </summary>
    public static FilterThresholds Default { get; } = new(
        PrimaryPathRatioTarget: 0.60,
        PrematureAbstractionRatioTarget: 0.25,
        MaxMethodComplexity: 5.0,
        MaxOnboardingHours: 40.0,
        PassingScore: 0.70);
}
