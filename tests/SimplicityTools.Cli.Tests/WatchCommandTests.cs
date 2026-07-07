using System.Globalization;
using SimplicityTools.Cli;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class WatchCommandTests : IDisposable
{
    private readonly string workspace;
    private readonly string solutionPath;

    public WatchCommandTests()
    {
        workspace = Path.Combine(Path.GetTempPath(), $"watch-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        solutionPath = Path.Combine(workspace, "Sample.sln");
        File.WriteAllText(solutionPath, string.Empty);
    }

    private static SimplicitySnapshot CreateSnapshot() =>
        new()
        {
            TotalProjects = 1,
            TotalFiles = 10,
            PrimaryPathFileCount = 5,
            AbstractionLayerCount = 0,
            ExternalDependencyCount = 0,
            UnusedDependencyCount = 0,
            InterfacesWithSingleImplementation = 0,
            AverageMethodComplexity = 1.0,
            EstimatedOnboardingTime = null,
            CollectedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task Debouncer_ForceFiresAfterMaxLatency_DespiteContinuousChurn()
    {
        using var fired = new SemaphoreSlim(0);
        using var debouncer = new WatchChangeDebouncer(
            TimeSpan.FromMilliseconds(200),
            _ =>
            {
                fired.Release();
                return Task.CompletedTask;
            },
            CancellationToken.None,
            maxLatency: TimeSpan.FromMilliseconds(600));

        // Signal every 50 ms: each signal postpones the 200 ms trailing debounce, so without a
        // latency cap the callback would never fire during churn.
        var change = new WatchChangeNotification(WatcherChangeTypes.Changed, "file.cs", null);
        using var churnCancellation = new CancellationTokenSource();
        var churn = Task.Run(async () =>
        {
            while (!churnCancellation.IsCancellationRequested)
            {
                debouncer.Signal(change);
                await Task.Delay(50);
            }
        });

        var firedDuringChurn = await fired.WaitAsync(TimeSpan.FromSeconds(3));
        churnCancellation.Cancel();
        await churn;

        Assert.True(firedDuringChurn, "Debouncer never force-fired while changes kept arriving.");
    }

    [Fact]
    public async Task Runner_CancelsInFlightCollection_AndShutsDownCleanly()
    {
        using var outputWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
        var collectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedCancellation = false;

        var runner = new WatchCommandRunner(
            solutionPath,
            outputWriter,
            errorWriter,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            collectSnapshotAsync: async (_, token) =>
            {
                collectorStarted.TrySetResult();
                try
                {
                    // Simulates a long collection; must be interruptible at shutdown.
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                }
                catch (OperationCanceledException)
                {
                    observedCancellation = true;
                    throw;
                }

                return CreateSnapshot();
            });

        using var cancellationSource = new CancellationTokenSource();
        var runTask = runner.RunAsync(cancellationSource.Token);

        await collectorStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellationSource.Cancel();

        var exitCode = await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, exitCode);
        Assert.True(observedCancellation, "The in-flight collection never observed the shutdown token.");
        Assert.DoesNotContain("ObjectDisposed", errorWriter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Runner_RearmsWatcherAndForcesReanalysis_AfterWatcherError()
    {
        using var outputWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
        var analysisCount = 0;

        var runner = new WatchCommandRunner(
            solutionPath,
            outputWriter,
            errorWriter,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            collectSnapshotAsync: (_, _) =>
            {
                Interlocked.Increment(ref analysisCount);
                return Task.FromResult(CreateSnapshot());
            });

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var runTask = runner.RunAsync(cancellationSource.Token);

        // Wait for the initial analysis, then simulate a watcher buffer overflow.
        while (Volatile.Read(ref analysisCount) < 1 && !cancellationSource.IsCancellationRequested)
        {
            await Task.Delay(25);
        }

        runner.SimulateWatcherError(new ErrorEventArgs(new InternalBufferOverflowException("buffer overflow")));

        while (Volatile.Read(ref analysisCount) < 2 && !cancellationSource.IsCancellationRequested)
        {
            await Task.Delay(25);
        }

        cancellationSource.Cancel();
        await runTask;

        Assert.True(analysisCount >= 2, "Watcher error did not trigger a catch-up analysis.");
        Assert.Contains("buffer overflow", errorWriter.ToString());
        Assert.Contains("Updated snapshot", outputWriter.ToString());
    }

    [Fact]
    public void CreateWatcher_UsesEnlargedInternalBuffer()
    {
        using var watcher = WatchCommandRunner.CreateWatcher(workspace);

        Assert.Equal(64 * 1024, watcher.InternalBufferSize);
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
