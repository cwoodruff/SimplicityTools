using SimplicityTools.Cli;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Cli.Tests;

/// <summary>
/// Issue #94: the terminal budget, the HTML report bands, and the simplicity score must agree on
/// the same run under the same configuration.
/// </summary>
public sealed class ScoringConsistencyTests
{
    private static SimplicitySnapshot CreateSnapshot(double averageMethodComplexity) =>
        new(
            TotalProjects: 2,
            TotalFiles: 20,
            PrimaryPathFileCount: 14,
            AbstractionLayerCount: 1,
            ExternalDependencyCount: 0,
            UnusedDependencyCount: 0,
            InterfacesWithSingleImplementation: 0,
            AverageMethodComplexity: averageMethodComplexity,
            EstimatedOnboardingTime: null,
            CollectedAt: DateTimeOffset.UnixEpoch);

    [Fact]
    public void BudgetAndReportBands_AgreeOnConfiguredComplexityThreshold()
    {
        // Complexity 4.0 with maxMethodComplexity 3: the terminal budget says OVER BUDGET, so the
        // HTML band must not say "Good"/"Excellent".
        var snapshot = CreateSnapshot(averageMethodComplexity: 4.0);
        var configuration = new FilterThresholdConfiguration(
            PrimaryPathRatioTarget: 0.60,
            PrematureAbstractionRatioTarget: 0.25,
            MaxMethodComplexity: 3.0,
            MaxOnboardingHours: 40,
            PassingScore: 0.70);

        var budget = ComplexityBudgetReportBuilder.Create(snapshot, configuration);
        var band = SimplicityScoring.GetComplexityBand(snapshot.AverageMethodComplexity, configuration.ToFilterThresholds());

        Assert.Contains("OVER BUDGET", budget);
        Assert.NotEqual(ScoreBandSeverity.Good, band.Severity);
    }

    [Fact]
    public void BudgetAndReportBands_AgreeWhenUnderConfiguredThreshold()
    {
        var snapshot = CreateSnapshot(averageMethodComplexity: 2.0);
        var configuration = new FilterThresholdConfiguration(
            PrimaryPathRatioTarget: 0.60,
            PrematureAbstractionRatioTarget: 0.25,
            MaxMethodComplexity: 5.0,
            MaxOnboardingHours: 40,
            PassingScore: 0.70);

        var budget = ComplexityBudgetReportBuilder.Create(snapshot, configuration);
        var band = SimplicityScoring.GetComplexityBand(snapshot.AverageMethodComplexity, configuration.ToFilterThresholds());

        Assert.Contains("Change Safety", budget);
        Assert.DoesNotContain("Change Safety      [##########]", budget);
        Assert.Equal(ScoreBandSeverity.Good, band.Severity);
    }
}
