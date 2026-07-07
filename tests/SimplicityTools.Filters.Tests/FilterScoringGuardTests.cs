using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Filters.Tests;

/// <summary>
/// Issue #98: a failed or empty collection must never be reported as an ideally simple codebase.
/// </summary>
public sealed class FilterScoringGuardTests
{
    private static SimplicitySnapshot CreateSnapshot(
        int totalProjects,
        int totalFiles,
        int abstractionLayerCount = 0,
        int interfacesWithSingleImplementation = 0,
        double averageMethodComplexity = 1) =>
        new(
            totalProjects,
            totalFiles,
            0,
            abstractionLayerCount,
            0,
            0,
            interfacesWithSingleImplementation,
            averageMethodComplexity,
            null,
            DateTimeOffset.UnixEpoch);

    [Fact]
    public void EmptySnapshot_FailsEveryFilter_WithExplicitViolation()
    {
        var empty = SimplicitySnapshot.Empty();

        foreach (var verdict in new[]
                 {
                     TwoAmTestEvaluator.Evaluate(empty),
                     HalfRuleEvaluator.Evaluate(empty),
                     PrimaryPathFirstEvaluator.Evaluate(empty)
                 })
        {
            Assert.False(verdict.Passes, $"{verdict.Filter} passed an empty snapshot.");
            Assert.Equal(0.0, verdict.Score);
            Assert.Contains(verdict.Violations, violation => violation.Contains("no measurable code", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(3, 0)]
    public void SnapshotWithZeroProjectsOrFiles_FailsEveryFilter(int totalProjects, int totalFiles)
    {
        var snapshot = CreateSnapshot(totalProjects, totalFiles);

        Assert.False(TwoAmTestEvaluator.Evaluate(snapshot).Passes);
        Assert.False(HalfRuleEvaluator.Evaluate(snapshot).Passes);
        Assert.False(PrimaryPathFirstEvaluator.Evaluate(snapshot).Passes);
    }

    [Fact]
    public void CatastrophicSubScore_BlocksAnOtherwisePassingVerdict()
    {
        // Sub-scores 1, 1, 1, ~0 average to 0.75 — above the 0.70 passing score, but a
        // collapsed dimension must not be waved through by the arithmetic mean.
        var verdict = FilterScoring.CreateVerdict(
            FilterName.TwoAmTest,
            passingScore: 0.70,
            [
                new FilterSubScore("A", 1.0),
                new FilterSubScore("B", 1.0),
                new FilterSubScore("C", 1.0),
                new FilterSubScore("D", 0.05)
            ],
            new Dictionary<string, string> { ["A"] = "a", ["B"] = "b", ["C"] = "c", ["D"] = "d" },
            new Dictionary<string, string> { ["A"] = "a", ["B"] = "b", ["C"] = "c", ["D"] = "d" });

        Assert.False(verdict.Passes);
        Assert.Contains("collapsed", verdict.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Violations_OnlyReportSubScoresBelowThePassingScore()
    {
        var verdict = FilterScoring.CreateVerdict(
            FilterName.HalfRule,
            passingScore: 0.70,
            [
                new FilterSubScore("Fine", 0.9),
                new FilterSubScore("Failing", 0.5)
            ],
            new Dictionary<string, string> { ["Fine"] = "fine violation", ["Failing"] = "failing violation" },
            new Dictionary<string, string> { ["Fine"] = "fine advice", ["Failing"] = "failing advice" });

        Assert.Equal(["failing violation"], verdict.Violations);
        Assert.Equal(["failing advice"], verdict.Recommendations);
    }

    [Fact]
    public void PassingVerdictWithNoFailingSubScores_HasNoViolationsOrRecommendations()
    {
        var verdict = FilterScoring.CreateVerdict(
            FilterName.HalfRule,
            passingScore: 0.70,
            [new FilterSubScore("A", 0.9), new FilterSubScore("B", 0.8)],
            new Dictionary<string, string> { ["A"] = "a", ["B"] = "b" },
            new Dictionary<string, string> { ["A"] = "a", ["B"] = "b" });

        Assert.True(verdict.Passes);
        Assert.Empty(verdict.Violations);
        Assert.Empty(verdict.Recommendations);
    }

    [Fact]
    public void NaNMetrics_ScoreZeroInsteadOfPropagating()
    {
        var snapshot = CreateSnapshot(totalProjects: 2, totalFiles: 10, averageMethodComplexity: double.NaN);

        var verdict = TwoAmTestEvaluator.Evaluate(snapshot);

        Assert.All(verdict.SubScores, subScore => Assert.False(double.IsNaN(subScore.Score)));
        Assert.False(double.IsNaN(verdict.Score));
    }

    [Fact]
    public void PrematureAbstractionRatio_IsOneWhenSingleImplInterfacesExistWithoutLayers()
    {
        var snapshot = CreateSnapshot(
            totalProjects: 2,
            totalFiles: 10,
            abstractionLayerCount: 0,
            interfacesWithSingleImplementation: 4);

        Assert.Equal(1.0, snapshot.PrematureAbstractionRatio);
    }
}
