using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Filters.Tests;

public sealed class FilterEvaluationTests
{
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
        // Only the sub-scores below the passing bar are violations (0.0 and 0.4; not 0.7).
        Assert.Equal(2, verdict.Violations.Count);
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

    [Fact]
    public void TwoAmTestAndPrimaryPathFirst_CanBothScorePerfect_OnLargeConcentratedSolutions()
    {
        // 100 files, 60 on the primary path, spread across 12 projects: exactly the shape
        // PrimaryPathFirst's concentration sub-score rewards. Discoverability must not collapse
        // just because the solution is large (issue #90: the old absolute 5-file target made the
        // two filters mathematically contradictory above ~8 files).
        var snapshot = CreateSnapshot(
            totalProjects: 12,
            totalFiles: 100,
            primaryPathFileCount: 60,
            abstractionLayerCount: 0,
            averageMethodComplexity: 2,
            estimatedOnboardingTime: TimeSpan.FromHours(10));

        var twoAm = TwoAmTestEvaluator.Evaluate(snapshot);
        var primaryPathFirst = PrimaryPathFirstEvaluator.Evaluate(snapshot);

        Assert.Equal(1.0, Assert.Single(twoAm.SubScores, subScore => subScore.Name == "Discoverability").Score);
        Assert.Equal(1.0, Assert.Single(primaryPathFirst.SubScores, subScore => subScore.Name == "Primary path concentration").Score);
    }

    [Fact]
    public void Discoverability_PenalizesBloatedPrimaryPathPerProject()
    {
        // 2 projects carrying 20 primary-path files (10 per project, target is 5 per project).
        var snapshot = CreateSnapshot(
            totalProjects: 2,
            totalFiles: 30,
            primaryPathFileCount: 20,
            abstractionLayerCount: 0,
            averageMethodComplexity: 2,
            estimatedOnboardingTime: TimeSpan.FromHours(10));

        var verdict = TwoAmTestEvaluator.Evaluate(snapshot);

        Assert.Equal(0.5, Assert.Single(verdict.SubScores, subScore => subScore.Name == "Discoverability").Score);
    }

    [Fact]
    public void Evaluators_HonorCustomThresholds()
    {
        // Perfect under the defaults.
        var snapshot = CreateSnapshot(
            totalProjects: 2,
            totalFiles: 20,
            primaryPathFileCount: 5,
            abstractionLayerCount: 6,
            averageMethodComplexity: 5,
            estimatedOnboardingTime: TimeSpan.FromHours(40));

        Assert.True(TwoAmTestEvaluator.Evaluate(snapshot).Passes);

        // Tighter complexity and onboarding targets drop the sub-scores; a raised passing score
        // turns the previously perfect snapshot into a failure.
        var strict = FilterThresholds.Default with
        {
            MaxMethodComplexity = 2.5,
            MaxOnboardingHours = 20,
            PassingScore = 0.95
        };

        var verdict = TwoAmTestEvaluator.Evaluate(snapshot, strict);

        Assert.False(verdict.Passes);
        Assert.Equal(0.5, Assert.Single(verdict.SubScores, subScore => subScore.Name == "Diagnosability").Score);
        Assert.Equal(0.5, Assert.Single(verdict.SubScores, subScore => subScore.Name == "Cognitive load").Score);
    }

    [Fact]
    public void PrimaryPathFirstEvaluator_HonorsCustomRatioTarget()
    {
        var snapshot = CreateSnapshot(totalProjects: 2, totalFiles: 10, primaryPathFileCount: 6);

        var relaxed = PrimaryPathFirstEvaluator.Evaluate(snapshot, FilterThresholds.Default with { PrimaryPathRatioTarget = 0.30 });
        var strict = PrimaryPathFirstEvaluator.Evaluate(snapshot, FilterThresholds.Default with { PrimaryPathRatioTarget = 0.90 });

        Assert.Equal(1.0, Assert.Single(relaxed.SubScores, subScore => subScore.Name == "Primary path concentration").Score);
        Assert.Equal(0.6 / 0.9, Assert.Single(strict.SubScores, subScore => subScore.Name == "Primary path concentration").Score, precision: 10);
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
        new()
        {
            TotalProjects = totalProjects,
            TotalFiles = totalFiles,
            PrimaryPathFileCount = primaryPathFileCount,
            AbstractionLayerCount = abstractionLayerCount,
            ExternalDependencyCount = externalDependencyCount,
            UnusedDependencyCount = unusedDependencyCount,
            InterfacesWithSingleImplementation = interfacesWithSingleImplementation,
            AverageMethodComplexity = averageMethodComplexity,
            EstimatedOnboardingTime = estimatedOnboardingTime ?? TimeSpan.FromHours(8),
            CollectedAt = DateTimeOffset.Parse("2026-04-29T21:22:50.867-04:00")
        };
}
