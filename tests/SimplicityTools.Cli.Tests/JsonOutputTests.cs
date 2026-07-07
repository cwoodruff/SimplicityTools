using System.Text.Json;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class JsonOutputTests
{
    private static SimplicitySnapshot CreateSnapshot(
        int totalFiles = 20,
        int primaryPathFileCount = 8,
        double averageMethodComplexity = 2.0d)
    {
        return new SimplicitySnapshot(
            2,
            totalFiles,
            primaryPathFileCount,
            3,
            1,
            0,
            1,
            averageMethodComplexity,
            null,
            new DateTimeOffset(2026, 07, 01, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void SerializeDiff_EmitsBaselineCurrentRegressionsAndFlag()
    {
        var baseline = CreateSnapshot(averageMethodComplexity: 2.0d);
        var current = CreateSnapshot(averageMethodComplexity: 3.0d);
        var result = SnapshotDiffReportBuilder.CreateResult(baseline, current);

        using var document = JsonDocument.Parse(CliJsonOutput.SerializeDiff(result));
        var root = document.RootElement;

        Assert.Equal(20, root.GetProperty("baseline").GetProperty("totalFiles").GetInt32());
        Assert.Equal(3.0d, root.GetProperty("current").GetProperty("averageMethodComplexity").GetDouble());
        Assert.True(root.GetProperty("hasRegression").GetBoolean());
        var regressions = root.GetProperty("regressions").EnumerateArray().Select(static element => element.GetString()).ToArray();
        Assert.Contains(regressions, static regression => regression!.Contains("AverageMethodComplexity increased", StringComparison.Ordinal));
    }

    [Fact]
    public void SerializeDiff_ReportsNoRegressionForIdenticalSnapshots()
    {
        var snapshot = CreateSnapshot();
        var result = SnapshotDiffReportBuilder.CreateResult(snapshot, snapshot);

        using var document = JsonDocument.Parse(CliJsonOutput.SerializeDiff(result));

        Assert.False(document.RootElement.GetProperty("hasRegression").GetBoolean());
        Assert.Empty(document.RootElement.GetProperty("regressions").EnumerateArray());
    }

    [Fact]
    public void SerializeBudget_EmitsDimensionsCountsAndNotScored()
    {
        var result = ComplexityBudgetReportBuilder.CreateResult(CreateSnapshot(), SimplicityConfiguration.Defaults.Filters);

        using var document = JsonDocument.Parse(CliJsonOutput.SerializeBudget(result));
        var root = document.RootElement;

        var dimensions = root.GetProperty("dimensions").EnumerateArray().ToArray();
        Assert.Equal(3, dimensions.Length);

        var changeSafety = dimensions.Single(static dimension => dimension.GetProperty("name").GetString() == "Change Safety");
        Assert.Equal("Average method complexity", changeSafety.GetProperty("metricLabel").GetString());
        Assert.Equal(2.0d, changeSafety.GetProperty("actual").GetDouble());
        Assert.Equal(5.0d, changeSafety.GetProperty("target").GetDouble());
        Assert.Equal(0.4d, changeSafety.GetProperty("usageFraction").GetDouble(), precision: 6);
        Assert.True(changeSafety.GetProperty("withinBudget").GetBoolean());

        Assert.Equal(
            root.GetProperty("dimensions").GetArrayLength(),
            root.GetProperty("withinBudgetCount").GetInt32() + root.GetProperty("overBudgetCount").GetInt32());
        Assert.Equal("Cognitive Load", root.GetProperty("notScored").EnumerateArray().Single().GetString());
    }

    [Fact]
    public void SerializeBudget_ScoresCognitiveLoadWhenOnboardingTimeIsComputed()
    {
        var snapshot = CreateSnapshot() with { EstimatedOnboardingTime = TimeSpan.FromHours(20) };
        var result = ComplexityBudgetReportBuilder.CreateResult(snapshot, SimplicityConfiguration.Defaults.Filters);

        using var document = JsonDocument.Parse(CliJsonOutput.SerializeBudget(result));
        var root = document.RootElement;

        Assert.Empty(root.GetProperty("notScored").EnumerateArray());
        var cognitiveLoad = root.GetProperty("dimensions").EnumerateArray()
            .Single(static dimension => dimension.GetProperty("name").GetString() == "Cognitive Load");
        Assert.Equal(20d, cognitiveLoad.GetProperty("actual").GetDouble());
    }

    [Fact]
    public void SerializeBudget_RepresentsInfiniteUsageAsNamedLiteral()
    {
        var snapshot = CreateSnapshot(primaryPathFileCount: 0);
        var result = ComplexityBudgetReportBuilder.CreateResult(snapshot, SimplicityConfiguration.Defaults.Filters);

        var json = CliJsonOutput.SerializeBudget(result);
        using var document = JsonDocument.Parse(json);

        var discoverability = document.RootElement.GetProperty("dimensions").EnumerateArray()
            .Single(static dimension => dimension.GetProperty("name").GetString() == "Discoverability");
        Assert.Equal("Infinity", discoverability.GetProperty("usageFraction").GetString());
    }

    [Fact]
    public async Task AnalyzeCommand_FormatJson_WritesOnlyTheSnapshotEnvelopeToStandardOutput()
    {
        var solutionPath = CliTestSupport.GetRepositoryPath("samples", "Sample.Simplified", "Sample.Simplified.sln");

        var result = await CliTestSupport.RunCliAsync("analyze", "--format", "json", solutionPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Info: using built-in defaults", result.StandardError);

        // stdout must be pure JSON: parse the entire stream and verify the envelope round-trips.
        using var document = JsonDocument.Parse(result.StandardOutput);
        Assert.Equal(SnapshotEnvelope.CurrentVersion, document.RootElement.GetProperty("version").GetInt32());
        var snapshot = SnapshotEnvelope.Deserialize(result.StandardOutput, "analyze --format json output");
        Assert.True(snapshot.TotalFiles > 0);
    }

    [Fact]
    public async Task DiffCommand_FormatJson_WritesStructuredComparisonToStandardOutput()
    {
        var workspace = CliTestSupport.CreateSampleWorkspace("Sample.Simplified");

        try
        {
            var solutionPath = Path.Combine(workspace, "Sample.Simplified.sln");
            var fabricatedBaseline = CreateSnapshot(totalFiles: 5, primaryPathFileCount: 2, averageMethodComplexity: 1.0d);
            await BaselineSnapshotFile.WriteAsync(solutionPath, fabricatedBaseline);

            var result = await CliTestSupport.RunCliAsync("diff", "--format", "json", solutionPath);

            Assert.Equal(0, result.ExitCode);
            using var document = JsonDocument.Parse(result.StandardOutput);
            var root = document.RootElement;
            Assert.Equal(5, root.GetProperty("baseline").GetProperty("totalFiles").GetInt32());
            // Files under the dot-prefixed .workspace directory are excluded from file counts,
            // so only the structurally-parsed project count is asserted for the current side.
            Assert.Equal(2, root.GetProperty("current").GetProperty("totalProjects").GetInt32());
            Assert.Equal(JsonValueKind.Array, root.GetProperty("regressions").ValueKind);
            Assert.True(root.GetProperty("hasRegression").ValueKind is JsonValueKind.True or JsonValueKind.False);
        }
        finally
        {
            CliTestSupport.DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task BudgetCommand_FormatJson_WritesDimensionArrayToStandardOutput()
    {
        var solutionPath = CliTestSupport.GetRepositoryPath("samples", "Sample.Simplified", "Sample.Simplified.sln");

        var result = await CliTestSupport.RunCliAsync("budget", "--format", "json", solutionPath);

        Assert.Equal(0, result.ExitCode);
        using var document = JsonDocument.Parse(result.StandardOutput);
        var root = document.RootElement;
        Assert.True(root.GetProperty("dimensions").GetArrayLength() >= 3);
        Assert.True(root.TryGetProperty("withinBudgetCount", out _));
        Assert.True(root.TryGetProperty("overBudgetCount", out _));
    }
}
