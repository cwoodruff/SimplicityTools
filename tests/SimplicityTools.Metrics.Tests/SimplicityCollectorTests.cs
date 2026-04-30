using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class SimplicityCollectorTests
{
    [Fact]
    public async Task CollectAsync_CountsProjectsFilesAndUniquePackages_FromStructuralFixture()
    {
        var collector = new SimplicityCollector();
        var before = DateTimeOffset.UtcNow;

        var snapshot = await collector.CollectAsync(GetFixturePath("StructuralFixture", "StructuralFixture.sln"));

        var after = DateTimeOffset.UtcNow;

        Assert.Equal(2, snapshot.TotalProjects);
        Assert.Equal(4, snapshot.TotalFiles);
        Assert.Equal(3, snapshot.ExternalDependencyCount);
        Assert.Equal(0, snapshot.PrimaryPathFileCount);
        Assert.Equal(0, snapshot.AbstractionLayerCount);
        Assert.Equal(3, snapshot.UnusedDependencyCount);
        Assert.Equal(0, snapshot.InterfacesWithSingleImplementation);
        Assert.Equal(1d, snapshot.AverageMethodComplexity);
        Assert.Equal(TimeSpan.Zero, snapshot.EstimatedOnboardingTime);
        Assert.InRange(snapshot.CollectedAt, before, after);
    }

    [Fact]
    public async Task CollectAsync_UsesFolderConventionAndInboundReferences_WhenNoAnnotationExists()
    {
        var collector = new SimplicityCollector();

        var snapshot = await collector.CollectAsync(GetFixturePath("PrimaryPathHeuristicFixture", "PrimaryPathHeuristicFixture.sln"));

        Assert.Equal(1, snapshot.TotalProjects);
        Assert.Equal(4, snapshot.TotalFiles);
        Assert.Equal(2, snapshot.PrimaryPathFileCount);
    }

    [Fact]
    public async Task CollectAsync_UsesAnnotationAsOverride_WhenPrimaryPathAttributeExists()
    {
        var collector = new SimplicityCollector();

        var snapshot = await collector.CollectAsync(GetFixturePath("PrimaryPathAnnotationFixture", "PrimaryPathAnnotationFixture.sln"));

        Assert.Equal(1, snapshot.TotalProjects);
        Assert.Equal(4, snapshot.TotalFiles);
        Assert.Equal(1, snapshot.PrimaryPathFileCount);
    }

    [Fact]
    public async Task CollectAsync_ComputesSemanticMetrics_FromSemanticFixture()
    {
        var collector = new SimplicityCollector();

        var snapshot = await collector.CollectAsync(GetFixturePath("SemanticFixture", "SemanticFixture.sln"));

        Assert.Equal(1, snapshot.TotalProjects);
        Assert.Equal(2, snapshot.TotalFiles);
        Assert.Equal(2, snapshot.ExternalDependencyCount);
        Assert.Equal(2, snapshot.AbstractionLayerCount);
        Assert.Equal(1, snapshot.UnusedDependencyCount);
        Assert.Equal(1, snapshot.InterfacesWithSingleImplementation);
        Assert.Equal(11d / 6d, snapshot.AverageMethodComplexity);
    }

    [Fact]
    public async Task CollectAsync_ScansCurrentSampleSolutions()
    {
        var collector = new SimplicityCollector();
        var repoRoot = GetRepositoryRoot();

        var simplified = await collector.CollectAsync(Path.Combine(repoRoot, "samples", "Sample.Simplified", "Sample.Simplified.sln"));
        var overEngineered = await collector.CollectAsync(Path.Combine(repoRoot, "samples", "Sample.OverEngineered", "Sample.OverEngineered.sln"));

        Assert.Equal(2, simplified.TotalProjects);
        Assert.Equal(12, overEngineered.TotalProjects);
        Assert.True(simplified.TotalFiles > 0);
        Assert.True(overEngineered.TotalFiles > simplified.TotalFiles);
        Assert.True(overEngineered.AbstractionLayerCount > simplified.AbstractionLayerCount);
        Assert.True(overEngineered.InterfacesWithSingleImplementation > simplified.InterfacesWithSingleImplementation);
    }

    private static string GetFixturePath(params string[] segments)
    {
        return Path.Combine(GetRepositoryRoot(), "tests", "SimplicityTools.Metrics.Tests", "TestData", Path.Combine(segments));
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SimplicityTools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root from the test base directory.");
    }
}
