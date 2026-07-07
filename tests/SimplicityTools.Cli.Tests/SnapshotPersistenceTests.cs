using SimplicityTools.Cli;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class SnapshotPersistenceTests : IDisposable
{
    private readonly string workspace;
    private readonly string solutionPath;

    public SnapshotPersistenceTests()
    {
        workspace = Path.Combine(Path.GetTempPath(), $"snapshot-persistence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        solutionPath = Path.Combine(workspace, "Sample.sln");
        File.WriteAllText(solutionPath, string.Empty);
    }

    private static SimplicitySnapshot CreateSnapshot(int totalFiles = 20) =>
        new(
            TotalProjects: 2,
            TotalFiles: totalFiles,
            PrimaryPathFileCount: 5,
            AbstractionLayerCount: 1,
            ExternalDependencyCount: 3,
            UnusedDependencyCount: 1,
            InterfacesWithSingleImplementation: 0,
            AverageMethodComplexity: 1.5,
            EstimatedOnboardingTime: null,
            CollectedAt: new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Baseline_WritesVersionedEnvelope_AndRoundTrips()
    {
        var snapshot = CreateSnapshot();

        var path = await BaselineSnapshotFile.WriteAsync(solutionPath, snapshot);
        var written = await File.ReadAllTextAsync(path);

        Assert.Contains("\"version\": 1", written);
        Assert.Contains("\"toolVersion\"", written);
        Assert.Contains("\"snapshot\"", written);

        var read = await BaselineSnapshotFile.ReadAsync(solutionPath);
        Assert.Equal(snapshot, read);
    }

    [Fact]
    public async Task Baseline_ReadsLegacyRawSnapshotFiles()
    {
        var legacyJson = """
        {
          "totalProjects": 2,
          "totalFiles": 24,
          "primaryPathFileCount": 5,
          "abstractionLayerCount": 1,
          "externalDependencyCount": 0,
          "unusedDependencyCount": 0,
          "interfacesWithSingleImplementation": 0,
          "averageMethodComplexity": 1.4,
          "estimatedOnboardingTime": "00:00:00",
          "collectedAt": "2026-05-01T00:00:00+00:00"
        }
        """;
        await File.WriteAllTextAsync(BaselineSnapshotFile.GetPath(solutionPath), legacyJson);

        var read = await BaselineSnapshotFile.ReadAsync(solutionPath);

        Assert.Equal(24, read.TotalFiles);
    }

    [Fact]
    public async Task Baseline_RejectsUnknownEnvelopeVersion()
    {
        await File.WriteAllTextAsync(
            BaselineSnapshotFile.GetPath(solutionPath),
            """{ "version": 99, "toolVersion": "9.9.9", "snapshot": {} }""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => BaselineSnapshotFile.ReadAsync(solutionPath));

        Assert.Contains("99", exception.Message);
        Assert.Contains("version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Baseline_RejectsSnapshotsWithMissingProperties_InsteadOfZeroFilling()
    {
        // An older tool's schema without unusedDependencyCount must not silently read as 0.
        var driftedJson = """
        {
          "totalProjects": 2,
          "totalFiles": 24,
          "primaryPathFileCount": 5,
          "abstractionLayerCount": 1,
          "externalDependencyCount": 0,
          "interfacesWithSingleImplementation": 0,
          "averageMethodComplexity": 1.4,
          "estimatedOnboardingTime": null,
          "collectedAt": "2026-05-01T00:00:00+00:00"
        }
        """;
        await File.WriteAllTextAsync(BaselineSnapshotFile.GetPath(solutionPath), driftedJson);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => BaselineSnapshotFile.ReadAsync(solutionPath));

        Assert.Contains("unusedDependencyCount", exception.Message);
    }

    [Fact]
    public async Task Baseline_RejectsSnapshotsWithUnknownProperties()
    {
        var futureJson = """
        {
          "version": 1,
          "toolVersion": "0.9.0",
          "snapshot": {
            "totalProjects": 2,
            "totalFiles": 24,
            "primaryPathFileCount": 5,
            "abstractionLayerCount": 1,
            "externalDependencyCount": 0,
            "unusedDependencyCount": 0,
            "interfacesWithSingleImplementation": 0,
            "averageMethodComplexity": 1.4,
            "estimatedOnboardingTime": null,
            "collectedAt": "2026-05-01T00:00:00+00:00",
            "someFutureMetric": 42
          }
        }
        """;
        await File.WriteAllTextAsync(BaselineSnapshotFile.GetPath(solutionPath), futureJson);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => BaselineSnapshotFile.ReadAsync(solutionPath));

        Assert.Contains("someFutureMetric", exception.Message);
    }

    [Fact]
    public async Task History_AppendWritesTimestampedEnvelope_AndReadReturnsIt()
    {
        var snapshot = CreateSnapshot();

        var path = await SnapshotHistory.AppendAsync(solutionPath, snapshot);

        Assert.Equal(".simplicity-history", Path.GetFileName(Path.GetDirectoryName(path)));
        Assert.Equal("2026-07-06T120000Z.json", Path.GetFileName(path));
        Assert.Contains("\"version\": 1", await File.ReadAllTextAsync(path));

        var snapshots = await SnapshotHistory.ReadAsync(solutionPath, TextWriter.Null);
        Assert.Single(snapshots);
        Assert.Equal(snapshot, snapshots[0]);
    }

    [Fact]
    public async Task History_AppendPrunesOldestBeyondRetentionLimit()
    {
        for (var day = 1; day <= 5; day++)
        {
            var snapshot = CreateSnapshot() with { CollectedAt = new DateTimeOffset(2026, 7, day, 12, 0, 0, TimeSpan.Zero) };
            await SnapshotHistory.AppendAsync(solutionPath, snapshot, retentionLimit: 3);
        }

        var files = Directory.GetFiles(SnapshotHistory.GetDirectoryPath(solutionPath), "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["2026-07-03T120000Z.json", "2026-07-04T120000Z.json", "2026-07-05T120000Z.json"], files);
    }

    [Fact]
    public async Task History_ReportsUnreadableFilesInsteadOfSilentlySkipping()
    {
        await SnapshotHistory.AppendAsync(solutionPath, CreateSnapshot());
        var corruptPath = Path.Combine(SnapshotHistory.GetDirectoryPath(solutionPath), "0000-corrupt.json");
        await File.WriteAllTextAsync(corruptPath, "{ not json");

        var diagnostics = new StringWriter();
        var snapshots = await SnapshotHistory.ReadAsync(solutionPath, diagnostics);

        Assert.Single(snapshots);
        Assert.Contains("0000-corrupt.json", diagnostics.ToString());
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(workspace, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
