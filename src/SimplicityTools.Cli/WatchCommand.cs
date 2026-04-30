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
    private readonly Func<string, Task<SimplicitySnapshot>> _collectSnapshotAsync;
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
        Func<string, Task<SimplicitySnapshot>>? collectSnapshotAsync = null)
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
            change => AnalyzeAndWriteAsync("Updated snapshot", change, CancellationToken.None),
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
            _watcher.Dispose();
            _debouncer.Dispose();
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
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.CreationTime
        };
    }

    private static Task<SimplicitySnapshot> CreateCollectorAsync(string solutionPath)
    {
        var collector = new SimplicityCollector();
        return collector.CollectAsync(solutionPath);
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
        _errorWriter.WriteLine($"Watch update failed: {message}");
    }

    private async Task AnalyzeAndWriteAsync(string heading, WatchChangeNotification? change, CancellationToken cancellationToken)
    {
        await _analysisGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            LoadConfiguration();
            var snapshot = await _collectSnapshotAsync(_solutionPath).ConfigureAwait(false);

            _outputWriter.WriteLine(heading);
            _outputWriter.WriteLine(new string('-', heading.Length));

            if (change is not null)
            {
                _outputWriter.WriteLine($"Change detected: {change.FormatForDisplay(_solutionDirectory)}");
            }

            _outputWriter.WriteLine(WatchAnalysisReportBuilder.Create(snapshot));
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

    private void LoadConfiguration()
    {
        var configPath = SimplicityConfigurationLoader.GetPathForSolution(_solutionPath);
        var configExists = File.Exists(configPath);
        _ = SimplicityConfigurationLoader.LoadForSolution(
            _solutionPath,
            _errorWriter,
            warnWhenMissing: !_hasWarnedAboutMissingConfiguration || configExists);
        _hasWarnedAboutMissingConfiguration = !configExists;
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
    private readonly TimeSpan _delay;
    private readonly Func<WatchChangeNotification, Task> _callbackAsync;
    private readonly CancellationToken _shutdownToken;
    private readonly object _syncLock = new();

    private CancellationTokenSource? _pendingSignalSource;

    public WatchChangeDebouncer(
        TimeSpan delay,
        Func<WatchChangeNotification, Task> callbackAsync,
        CancellationToken shutdownToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Debounce delay must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(callbackAsync);

        _delay = delay;
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

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(_delay, signalToken).ConfigureAwait(false);
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
    public static string Create(SimplicitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var builder = new StringBuilder();
        builder.AppendLine(snapshot.ToSummary());
        builder.AppendLine();
        builder.AppendLine("Filter Verdicts");
        builder.AppendLine("---------------");

        foreach (var verdict in Evaluate(snapshot))
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

    private static IReadOnlyList<FilterVerdict> Evaluate(SimplicitySnapshot snapshot)
    {
        return
        [
            TwoAmTestEvaluator.Evaluate(snapshot),
            HalfRuleEvaluator.Evaluate(snapshot),
            PrimaryPathFirstEvaluator.Evaluate(snapshot)
        ];
    }
}
