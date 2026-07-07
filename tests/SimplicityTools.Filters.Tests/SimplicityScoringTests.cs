using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Filters.Tests;

public sealed class SimplicityScoringTests
{
    private static SimplicitySnapshot CreateSnapshot(
        int totalFiles = 100,
        int primaryPathFileCount = 80,
        int abstractionLayerCount = 0,
        int unusedDependencyCount = 0,
        int interfacesWithSingleImplementation = 0,
        double averageMethodComplexity = 2.0) =>
        new()
        {
            TotalProjects = 4,
            TotalFiles = totalFiles,
            PrimaryPathFileCount = primaryPathFileCount,
            AbstractionLayerCount = abstractionLayerCount,
            ExternalDependencyCount = unusedDependencyCount,
            UnusedDependencyCount = unusedDependencyCount,
            InterfacesWithSingleImplementation = interfacesWithSingleImplementation,
            AverageMethodComplexity = averageMethodComplexity,
            EstimatedOnboardingTime = null,
            CollectedAt = DateTimeOffset.UnixEpoch
        };

    [Fact]
    public void CalculateScore_WithDefaultThresholds_MatchesLegacyFormula()
    {
        // Legacy formula: 100 - ratio*30 - min(unused*3,20)
        //                     - max(0,(complexity-3)/10*20) - max(0,(0.8-primaryRatio)/0.8*30)
        var snapshot = CreateSnapshot(
            totalFiles: 100,
            primaryPathFileCount: 40,          // ratio 0.4 -> primary-path penalty (0.8-0.4)/0.8*30 = 15
            abstractionLayerCount: 10,
            interfacesWithSingleImplementation: 5, // premature ratio 0.5 -> penalty 15
            unusedDependencyCount: 2,          // penalty 6
            averageMethodComplexity: 8);       // penalty (8-3)/10*20 = 10

        var score = SimplicityScoring.CalculateScore(snapshot, FilterThresholds.Default);

        Assert.Equal(100 - 15 - 6 - 10 - 15, score, precision: 10);
    }

    [Fact]
    public void CalculateScore_IsClampedToZeroToOneHundred()
    {
        var terrible = CreateSnapshot(
            totalFiles: 100,
            primaryPathFileCount: 0,
            abstractionLayerCount: 10,
            interfacesWithSingleImplementation: 10,
            unusedDependencyCount: 50,
            averageMethodComplexity: 40);

        var pristine = CreateSnapshot();

        Assert.Equal(0, SimplicityScoring.CalculateScore(terrible, FilterThresholds.Default));
        Assert.Equal(100, SimplicityScoring.CalculateScore(pristine, FilterThresholds.Default));
    }

    [Fact]
    public void CalculateScore_HonorsConfiguredThresholds()
    {
        var snapshot = CreateSnapshot(averageMethodComplexity: 4.0);

        var lenient = SimplicityScoring.CalculateScore(snapshot, FilterThresholds.Default);
        var strict = SimplicityScoring.CalculateScore(
            snapshot,
            FilterThresholds.Default with { MaxMethodComplexity = 2.0 });

        // Complexity 4 is under the default target of 5 (small penalty) but double a strict
        // target of 2 (large penalty).
        Assert.True(strict < lenient);
    }

    [Theory]
    [InlineData(2.9, "Excellent", ScoreBandSeverity.Good)]
    [InlineData(4.9, "Good", ScoreBandSeverity.Good)]
    [InlineData(9.9, "Moderate", ScoreBandSeverity.Warn)]
    [InlineData(10.1, "High", ScoreBandSeverity.Critical)]
    public void GetComplexityBand_WithDefaults_MatchesLegacyReportBands(double complexity, string label, ScoreBandSeverity severity)
    {
        var band = SimplicityScoring.GetComplexityBand(complexity, FilterThresholds.Default);

        Assert.Equal(label, band.Label);
        Assert.Equal(severity, band.Severity);
    }

    [Fact]
    public void GetComplexityBand_ShiftsWithConfiguredThreshold()
    {
        // A team that sets maxMethodComplexity: 3 must not see "Good" for complexity 4 in the
        // HTML report while the terminal budget says OVER BUDGET (issue #94).
        var strict = FilterThresholds.Default with { MaxMethodComplexity = 3.0 };

        var band = SimplicityScoring.GetComplexityBand(4.0, strict);

        Assert.NotEqual("Good", band.Label);
        Assert.NotEqual(ScoreBandSeverity.Good, band.Severity);
    }

    [Theory]
    [InlineData(0.85, "Good", ScoreBandSeverity.Good)]
    [InlineData(0.6, "Review", ScoreBandSeverity.Warn)]
    [InlineData(0.4, "Critical", ScoreBandSeverity.Critical)]
    public void GetPrimaryPathBand_WithDefaults_MatchesLegacyReportBadges(double ratio, string label, ScoreBandSeverity severity)
    {
        var band = SimplicityScoring.GetPrimaryPathBand(ratio, FilterThresholds.Default);

        Assert.Equal(label, band.Label);
        Assert.Equal(severity, band.Severity);
    }

    [Theory]
    [InlineData(0.2, "Good", ScoreBandSeverity.Good)]
    [InlineData(0.5, "Review", ScoreBandSeverity.Warn)]
    [InlineData(0.7, "Critical", ScoreBandSeverity.Critical)]
    public void GetPrematureAbstractionBand_WithDefaults_MatchesLegacyReportBadges(double ratio, string label, ScoreBandSeverity severity)
    {
        var band = SimplicityScoring.GetPrematureAbstractionBand(ratio, FilterThresholds.Default);

        Assert.Equal(label, band.Label);
        Assert.Equal(severity, band.Severity);
    }

    [Theory]
    [InlineData(10.0, "Efficient", ScoreBandSeverity.Good)]
    [InlineData(20.0, "Moderate", ScoreBandSeverity.Warn)]
    [InlineData(50.0, "Substantial", ScoreBandSeverity.Critical)]
    public void GetOnboardingBand_WithDefaults_MatchesLegacyReportBands(double hours, string label, ScoreBandSeverity severity)
    {
        var band = SimplicityScoring.GetOnboardingBand(hours, FilterThresholds.Default);

        Assert.Equal(label, band.Label);
        Assert.Equal(severity, band.Severity);
    }
}
