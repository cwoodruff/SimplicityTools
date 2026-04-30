using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Filters.Tests;

public sealed class FilterEvaluationTests
{
    [Fact]
    public void Evaluation_HoldsSnapshotReference()
    {
        var snapshot = SimplicitySnapshot.Empty("Sample");
        var evaluation = new FilterEvaluation("2AM", Passed: true, "Placeholder", snapshot);

        Assert.Same(snapshot, evaluation.Snapshot);
    }

    [Fact]
    public void TwoAmTestEvaluator_ReturnsPerfectScoreWhenTargetsAreMet()
    {
        var snapshot = CreateSnapshot(
            totalProjects: 2,
            totalFiles: 20,
            primaryPathFileCount: 5,
            abstractionLayerCount: 6,
            averageMethodComplexity: 5,
            estimatedOnboardingTime: TimeSpan.FromHours(40));

        var verdict = TwoAmTestEvaluator.Evaluate(snapshot);

        Assert.Equal(FilterName.TwoAmTest, verdict.Filter);
        Assert.True(verdict.Passes);
        Assert.Equal(1.0, verdict.Score);
        Assert.Empty(verdict.Violations);
        Assert.Empty(verdict.Recommendations);
        Assert.All(verdict.SubScores, subScore => Assert.Equal(1.0, subScore.Score));
    }

    [Fact]
    public void HalfRuleEvaluator_UsesArithmeticMeanAndChoosesHighestImpactRecommendation()
    {
        var snapshot = CreateSnapshot(
            totalProjects: 2,
            abstractionLayerCount: 10,
            externalDependencyCount: 40,
            unusedDependencyCount: 3,
            interfacesWithSingleImplementation: 6);

        var verdict = HalfRuleEvaluator.Evaluate(snapshot);

        Assert.Equal(FilterName.HalfRule, verdict.Filter);
        Assert.False(verdict.Passes);
        Assert.Equal((0.0 + 0.7 + 0.4) / 3.0, verdict.Score, precision: 10);
        Assert.Collection(
            verdict.SubScores,
            subScore => Assert.Equal(("Premature abstraction", 0.0), (subScore.Name, subScore.Score)),
            subScore => Assert.Equal(("Dependency accumulation", 0.7), (subScore.Name, subScore.Score)),
            subScore => Assert.Equal(("Dependency sprawl", 0.4), (subScore.Name, subScore.Score)));
        Assert.Equal(3, verdict.Violations.Length);
        Assert.Single(verdict.Recommendations);
        Assert.Contains("single-implementation interfaces", verdict.Recommendations[0]);
        AssertAllScoresAreBounded(verdict);
    }

    [Fact]
    public void HalfRuleEvaluator_PassesWhenCompositeScoreEqualsThreshold()
    {
        var snapshot = CreateSnapshot(
            totalProjects: 1,
            abstractionLayerCount: 4,
            externalDependencyCount: 80,
            interfacesWithSingleImplementation: 1);

        var verdict = HalfRuleEvaluator.Evaluate(snapshot);

        Assert.Equal(0.7, verdict.Score, precision: 10);
        Assert.True(verdict.Passes);
        AssertAllScoresAreBounded(verdict);
    }

    [Fact]
    public void PrimaryPathFirstEvaluator_ComputesExpectedSubScores()
    {
        var snapshot = CreateSnapshot(
            totalProjects: 7,
            totalFiles: 60,
            primaryPathFileCount: 36,
            abstractionLayerCount: 12);

        var verdict = PrimaryPathFirstEvaluator.Evaluate(snapshot);

        Assert.Equal(FilterName.PrimaryPathFirst, verdict.Filter);
        Assert.True(verdict.Passes);
        Assert.Equal((1.0 + 1.0 + (5.0 / 7.0)) / 3.0, verdict.Score, precision: 10);
        Assert.Collection(
            verdict.SubScores,
            subScore => Assert.Equal(("Primary path concentration", 1.0), (subScore.Name, subScore.Score)),
            subScore => Assert.Equal(("Abstraction dilution", 1.0), (subScore.Name, subScore.Score)),
            subScore => Assert.Equal(("Project count", 5.0 / 7.0), (subScore.Name, subScore.Score)));
        AssertAllScoresAreBounded(verdict);
    }

    private static void AssertAllScoresAreBounded(FilterVerdict verdict)
    {
        Assert.InRange(verdict.Score, 0.0, 1.0);
        Assert.All(verdict.SubScores, subScore => Assert.InRange(subScore.Score, 0.0, 1.0));
    }

    private static SimplicitySnapshot CreateSnapshot(
        int totalProjects = 1,
        int totalFiles = 10,
        int primaryPathFileCount = 10,
        int abstractionLayerCount = 0,
        int externalDependencyCount = 0,
        int unusedDependencyCount = 0,
        int interfacesWithSingleImplementation = 0,
        double averageMethodComplexity = 1,
        TimeSpan? estimatedOnboardingTime = null) =>
        new(
            totalProjects,
            totalFiles,
            primaryPathFileCount,
            abstractionLayerCount,
            externalDependencyCount,
            unusedDependencyCount,
            interfacesWithSingleImplementation,
            averageMethodComplexity,
            estimatedOnboardingTime ?? TimeSpan.FromHours(8),
            DateTimeOffset.Parse("2026-04-29T21:22:50.867-04:00"));
}
