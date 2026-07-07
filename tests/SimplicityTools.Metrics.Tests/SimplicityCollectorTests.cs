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
        Assert.Null(snapshot.EstimatedOnboardingTime);
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
    public async Task CollectAsync_UnionsAnnotationsWithConventionMatches_WhenPrimaryPathAttributeExists()
    {
        var collector = new SimplicityCollector();

        var snapshot = await collector.CollectAsync(GetFixturePath("PrimaryPathAnnotationFixture", "PrimaryPathAnnotationFixture.sln"));

        Assert.Equal(1, snapshot.TotalProjects);
        Assert.Equal(4, snapshot.TotalFiles);
        Assert.Equal(2, snapshot.PrimaryPathFileCount);
    }

    [Theory]
    [InlineData(new int[0], null)]
    [InlineData(new[] { 0, 0 }, null)]
    [InlineData(new[] { 2, 2, 2 }, null)]
    [InlineData(new[] { 0, 0, 3 }, 3)]
    [InlineData(new[] { 1, 2, 3, 4 }, 3)]
    public void GetTopQuartileThreshold_ReturnsNullForDegenerateDistributions(int[] counts, int? expected)
    {
        Assert.Equal(expected, HeuristicCollectionPass.GetTopQuartileThreshold(counts));
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
    public async Task CollectAsync_CountsEachMetricOnce_ForMultiTargetedProjects()
    {
        var collector = new SimplicityCollector();

        var snapshot = await collector.CollectAsync(GetFixturePath("MultiTargetFixture", "MultiTargetFixture.sln"));

        Assert.Equal(1, snapshot.TotalProjects);
        Assert.Equal(2, snapshot.TotalFiles);
        Assert.Equal(1, snapshot.AbstractionLayerCount);
        Assert.Equal(1, snapshot.InterfacesWithSingleImplementation);
        Assert.Equal(3d, snapshot.AverageMethodComplexity);
    }

    [Fact]
    public async Task CollectAsync_ReportsWorkspaceDiagnostics_WhenAProjectFailsToLoad()
    {
        var diagnostics = new List<string>();
        var collector = new SimplicityCollector(diagnostics.Add);

        var snapshot = await collector.CollectAsync(GetFixturePath("BrokenReferenceFixture", "BrokenReferenceFixture.sln"));

        Assert.Equal(2, snapshot.TotalProjects);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Contains("Ghost", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Contains("1 of 2", StringComparison.Ordinal));
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
        // TotalFiles counts the analyzed population: countable source files outside test
        // projects — the same population the primary-path numerator is drawn from.
        Assert.Equal(19, simplified.TotalFiles);
        Assert.True(overEngineered.TotalFiles > simplified.TotalFiles);
        Assert.True(overEngineered.AbstractionLayerCount > simplified.AbstractionLayerCount);
        Assert.True(overEngineered.InterfacesWithSingleImplementation > simplified.InterfacesWithSingleImplementation);
    }

    [Fact]
    public async Task CollectAsync_DoesNotSpawnRestore_WhenAllowRestoreIsDisabled()
    {
        // A fresh copy of a fixture that declares packages, with no obj/ directories: the
        // default behavior would spawn `dotnet restore` here and write project.assets.json.
        var workspaceDirectory = Path.Combine(Path.GetTempPath(), "simplicity-collector-tests", Guid.NewGuid().ToString("N"));
        CopyDirectoryWithoutBuildOutput(GetFixturePath("SemanticFixture"), workspaceDirectory);

        try
        {
            var assetsPath = Path.Combine(workspaceDirectory, "SemanticFixture.App", "obj", "project.assets.json");
            Assert.False(File.Exists(assetsPath));

            var collector = new SimplicityCollector(new SimplicityCollectorOptions { AllowRestore = false });

            var snapshot = await collector.CollectAsync(Path.Combine(workspaceDirectory, "SemanticFixture.sln"));

            Assert.False(File.Exists(assetsPath), "AllowRestore=false must not spawn a restore or write package assets.");
            Assert.Equal(1, snapshot.TotalProjects);
            // Without package assets the unused-dependency pass skips the project instead of guessing.
            Assert.Equal(0, snapshot.UnusedDependencyCount);
        }
        finally
        {
            Directory.Delete(workspaceDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CollectAsync_Succeeds_WhenLocatorRegistrationIsDisabledAndAlreadyRegistered()
    {
        // First collection with default options registers MSBuild for the test process; the
        // second one opts out of registration and must still work against the registered instance.
        var registeringCollector = new SimplicityCollector();
        _ = await registeringCollector.CollectAsync(GetFixturePath("StructuralFixture", "StructuralFixture.sln"));

        var collector = new SimplicityCollector(new SimplicityCollectorOptions { RegisterMSBuildLocator = false });

        var snapshot = await collector.CollectAsync(GetFixturePath("StructuralFixture", "StructuralFixture.sln"));

        Assert.Equal(2, snapshot.TotalProjects);
        Assert.Equal(4, snapshot.TotalFiles);
    }

    private static void CopyDirectoryWithoutBuildOutput(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Contains("obj", StringComparer.OrdinalIgnoreCase) ||
                segments.Contains("bin", StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath);
        }
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
