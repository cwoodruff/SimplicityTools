using System.Text;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal sealed class WatchCommandRunner
{
    private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromMilliseconds(500);

    private readonly string _solutionPath;
    private readonly string _solutionDirectory;
    private readonly TextWriter _outputWriter;
    private readonly TextWriter _errorWriter;
    private readonly Func<string, CancellationToken, Task<SimplicitySnapshot>> _collectSnapshotAsync;
    private readonly WatchChangeDebouncer _debouncer;
    private readonly CancellationTokenSource _shutdownSource = new();
    private readonly SemaphoreSlim _analysisGate = new(1, 1);

    private FileSystemWatcher? _watcher;
    private bool _hasWarnedAboutMissingConfiguration;

    public WatchCommandRunner(
        string solutionPath,
        TextWriter outputWriter,
        TextWriter errorWriter,
        TimeSpan? debounceDelay = null,
        Func<string, CancellationToken, Task<SimplicitySnapshot>>? collectSnapshotAsync = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);
        ArgumentNullException.ThrowIfNull(outputWriter);
        ArgumentNullException.ThrowIfNull(errorWriter);

        _solutionPath = Path.GetFullPath(solutionPath);
        if (!File.Exists(_solutionPath))
        {
            throw new FileNotFoundException($"Solution file was not found at '{_solutionPath}'.", _solutionPath);
        }

        _solutionDirectory = Path.GetDirectoryName(_solutionPath)
            ?? throw new InvalidOperationException($"Could not determine the directory for '{solutionPath}'.");
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
        _collectSnapshotAsync = collectSnapshotAsync ?? CreateCollectorAsync;
        _debouncer = new WatchChangeDebouncer(
            debounceDelay ?? DefaultDebounceDelay,
            change => AnalyzeAndWriteAsync("Updated snapshot", change, _shutdownSource.Token),
            _shutdownSource.Token);
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdownSource.Token);
        var linkedToken = linkedSource.Token;

        _watcher = CreateWatcher(_solutionDirectory);
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnWatcherError;
        _watcher.EnableRaisingEvents = true;

        try
        {
            _outputWriter.WriteLine($"Watching {_solutionPath}");
            _outputWriter.WriteLine("Press Ctrl+C to stop.");
            _outputWriter.WriteLine();

            await AnalyzeAndWriteAsync("Initial snapshot", change: null, linkedToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, linkedToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
            }

            return 0;
        }
        finally
        {
            _shutdownSource.Cancel();
            _watcher?.Dispose();
            _debouncer.Dispose();

            // An in-flight debounced analysis observes the shutdown token above; wait for it to
            // drain before disposing the gate it still holds.
            await _analysisGate.WaitAsync().ConfigureAwait(false);
            _analysisGate.Release();

            _analysisGate.Dispose();
            _shutdownSource.Dispose();
        }
    }

    internal static FileSystemWatcher CreateWatcher(string solutionDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionDirectory);

        return new FileSystemWatcher(solutionDirectory)
        {
            IncludeSubdirectories = true,
            Filter = "*",
            // The 8 KB default overflows quickly when watching a whole tree; overflowed events
            // are lost silently.
            InternalBufferSize = 64 * 1024,
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.CreationTime
        };
    }

    private Task<SimplicitySnapshot> CreateCollectorAsync(string solutionPath, CancellationToken cancellationToken)
    {
        var collector = new SimplicityCollector(message => _errorWriter.WriteLine(message));
        return collector.CollectAsync(solutionPath, cancellationToken);
    }

    private void OnChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (ShouldIgnorePath(eventArgs.FullPath))
        {
            return;
        }

        _debouncer.Signal(new WatchChangeNotification(eventArgs.ChangeType, eventArgs.FullPath, PreviousFullPath: null));
    }

    private void OnRenamed(object sender, RenamedEventArgs eventArgs)
    {
        if (ShouldIgnorePath(eventArgs.FullPath) && ShouldIgnorePath(eventArgs.OldFullPath))
        {
            return;
        }

        _debouncer.Signal(new WatchChangeNotification(eventArgs.ChangeType, eventArgs.FullPath, eventArgs.OldFullPath));
    }

    private void OnWatcherError(object sender, ErrorEventArgs eventArgs)
    {
        var message = eventArgs.GetException().Message;
        _errorWriter.WriteLine($"File watcher error: {message}. Re-arming the watcher and re-analyzing to catch up on lost events.");

        if (_shutdownSource.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var failedWatcher = Interlocked.Exchange(ref _watcher, null);
            failedWatcher?.Dispose();

            var replacement = CreateWatcher(_solutionDirectory);
            replacement.Changed += OnChanged;
            replacement.Created += OnChanged;
            replacement.Deleted += OnChanged;
            replacement.Renamed += OnRenamed;
            replacement.Error += OnWatcherError;
            replacement.EnableRaisingEvents = true;
            _watcher = replacement;
        }
        catch (Exception exception)
        {
            _errorWriter.WriteLine($"Could not re-arm the file watcher: {exception.Message}");
        }

        // Events were lost while the watcher was down, so a full re-analysis is forced rather
        // than trusting the next incremental notification.
        _debouncer.Signal(new WatchChangeNotification(WatcherChangeTypes.All, _solutionDirectory, PreviousFullPath: null));
    }

    internal void SimulateWatcherError(ErrorEventArgs eventArgs) => OnWatcherError(this, eventArgs);

    private async Task AnalyzeAndWriteAsync(string heading, WatchChangeNotification? change, CancellationToken cancellationToken)
    {
        await _analysisGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var configuration = LoadConfiguration();
            var snapshot = await _collectSnapshotAsync(_solutionPath, cancellationToken).ConfigureAwait(false);

            _outputWriter.WriteLine(heading);
            _outputWriter.WriteLine(new string('-', heading.Length));

            if (change is not null)
            {
                _outputWriter.WriteLine($"Change detected: {change.FormatForDisplay(_solutionDirectory)}");
            }

            _outputWriter.WriteLine(WatchAnalysisReportBuilder.Create(snapshot, configuration.Filters.ToFilterThresholds()));
            _outputWriter.WriteLine();
            await _outputWriter.FlushAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _errorWriter.WriteLine($"Watch update failed: {exception.Message}");
            await _errorWriter.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _analysisGate.Release();
        }
    }

    private SimplicityConfiguration LoadConfiguration()
    {
        var configPath = SimplicityConfigurationLoader.GetPathForSolution(_solutionPath);
        var configExists = File.Exists(configPath);
        var configuration = SimplicityConfigurationLoader.LoadForSolution(
            _solutionPath,
            _errorWriter,
            warnWhenMissing: !_hasWarnedAboutMissingConfiguration || configExists);
        _hasWarnedAboutMissingConfiguration = !configExists;
        return configuration;
    }

    private bool ShouldIgnorePath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return true;
        }

        var relativePath = Path.GetRelativePath(_solutionDirectory, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal))
        {
            return true;
        }

        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var segments = relativePath.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(static segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("simplicity-report", StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class WatchChangeDebouncer : IDisposable
{
    private static readonly TimeSpan DefaultMaxLatency = TimeSpan.FromSeconds(5);

    private readonly TimeSpan _delay;
    private readonly TimeSpan _maxLatency;
    private readonly Func<WatchChangeNotification, Task> _callbackAsync;
    private readonly CancellationToken _shutdownToken;
    private readonly object _syncLock = new();

    private CancellationTokenSource? _pendingSignalSource;
    private DateTime? _firstPostponedSignalUtc;

    public WatchChangeDebouncer(
        TimeSpan delay,
        Func<WatchChangeNotification, Task> callbackAsync,
        CancellationToken shutdownToken,
        TimeSpan? maxLatency = null)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Debounce delay must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(callbackAsync);

        _delay = delay;
        _maxLatency = maxLatency ?? DefaultMaxLatency;
        if (_maxLatency < _delay)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLatency), "Max latency must be at least the debounce delay.");
        }

        _callbackAsync = callbackAsync;
        _shutdownToken = shutdownToken;
    }

    public void Signal(WatchChangeNotification change)
    {
        lock (_syncLock)
        {
            _pendingSignalSource?.Cancel();
            _pendingSignalSource?.Dispose();
            _pendingSignalSource = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
            var signalToken = _pendingSignalSource.Token;

            // Trailing-edge debounce with a latency cap: continuous churn keeps postponing the
            // trailing delay, so once the first postponed signal is older than the cap the
            // callback force-fires instead of being postponed again.
            _firstPostponedSignalUtc ??= DateTime.UtcNow;
            var remainingBeforeForcedFire = _firstPostponedSignalUtc.Value + _maxLatency - DateTime.UtcNow;
            var delay = remainingBeforeForcedFire < _delay
                ? (remainingBeforeForcedFire > TimeSpan.Zero ? remainingBeforeForcedFire : TimeSpan.Zero)
                : _delay;

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        if (delay > TimeSpan.Zero)
                        {
                            await Task.Delay(delay, signalToken).ConfigureAwait(false);
                        }

                        lock (_syncLock)
                        {
                            _firstPostponedSignalUtc = null;
                        }

                        await _callbackAsync(change).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (signalToken.IsCancellationRequested)
                    {
                    }
                },
                CancellationToken.None);
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            _pendingSignalSource?.Cancel();
            _pendingSignalSource?.Dispose();
            _pendingSignalSource = null;
        }
    }
}

internal sealed record WatchChangeNotification(WatcherChangeTypes ChangeType, string FullPath, string? PreviousFullPath)
{
    public string FormatForDisplay(string solutionDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionDirectory);

        var currentPath = Path.GetRelativePath(solutionDirectory, FullPath);
        if (ChangeType == WatcherChangeTypes.Renamed && !string.IsNullOrWhiteSpace(PreviousFullPath))
        {
            var previousPath = Path.GetRelativePath(solutionDirectory, PreviousFullPath);
            return $"{ChangeType}: {previousPath} -> {currentPath}";
        }

        return $"{ChangeType}: {currentPath}";
    }
}

internal static class WatchAnalysisReportBuilder
{
    public static string Create(SimplicitySnapshot snapshot, FilterThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var builder = new StringBuilder();
        builder.AppendLine(snapshot.ToSummary());
        builder.AppendLine();
        builder.AppendLine("Filter Verdicts");
        builder.AppendLine("---------------");

        var verdicts = SnapshotFilterEvaluation.Evaluate(snapshot, thresholds ?? FilterThresholds.Default);
        foreach (var verdict in SnapshotFilterEvaluation.GetFilterOrder().Select(filter => verdicts[filter]))
        {
            builder.AppendLine($"{verdict.Filter}: {(verdict.Passes ? "PASS" : "FAIL")} ({verdict.Score:F2})");
            builder.AppendLine($"  {verdict.Summary}");

            foreach (var violation in verdict.Violations)
            {
                builder.AppendLine($"  - {violation}");
            }

            foreach (var recommendation in verdict.Recommendations)
            {
                builder.AppendLine($"  Next move: {recommendation}");
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

}
