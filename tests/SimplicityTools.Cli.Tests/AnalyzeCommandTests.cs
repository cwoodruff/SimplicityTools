using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimplicityTools.Cli;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class AnalyzeCommandTests
{
    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static bool cliBuilt;

    [Fact]
    public async Task Collector_MatchesSampleBaselines()
    {
        var baselines = await LoadBaselinesAsync();
        var collector = new SimplicityCollector();

        foreach (var sample in baselines.Samples)
        {
            var snapshot = await collector.CollectAsync(GetRepositoryPath(sample.SolutionPath));

            Assert.Equal(sample.TotalProjects, snapshot.TotalProjects);
            Assert.Equal(sample.TotalFiles, snapshot.TotalFiles);
            Assert.Equal(sample.PrimaryPathFileCount, snapshot.PrimaryPathFileCount);
            Assert.Equal(sample.AbstractionLayerCount, snapshot.AbstractionLayerCount);
            Assert.Equal(sample.ExternalDependencyCount, snapshot.ExternalDependencyCount);
            Assert.Equal(sample.UnusedDependencyCount, snapshot.UnusedDependencyCount);
            Assert.Equal(sample.InterfacesWithSingleImplementation, snapshot.InterfacesWithSingleImplementation);
            AssertWithinTolerance(sample.AverageMethodComplexity, snapshot.AverageMethodComplexity, 0.05d, sample.Name, nameof(snapshot.AverageMethodComplexity));
            AssertTimeSpanWithinTolerance(sample.EstimatedOnboardingTime, snapshot.EstimatedOnboardingTime, 0.10d, sample.Name);
        }
    }

    [Fact]
    public async Task AnalyzeCommand_PrintsSummaryForBothSampleSolutions()
    {
        await BuildCliAsync();
        var baselines = await LoadBaselinesAsync();

        foreach (var sample in baselines.Samples)
        {
            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "analyze", GetRepositoryPath(sample.SolutionPath)],
                GetRepositoryRoot());

            Assert.Equal(0, result.ExitCode);
            AssertMissingConfigWarning(result.StandardError, Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!);

            var actual = NormalizeLineEndings(result.StandardOutput).Trim();
            var summaryDate = ParseSummaryDate(actual);
            var expected = sample.ToSnapshot(summaryDate).ToSummary();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public async Task UnknownCommand_WritesUsageToStandardErrorAndReturnsNonZeroExitCode()
    {
        await BuildCliAsync();

        var result = await RunProcessAsync(
            "dotnet",
            [GetCliAssemblyPath(), "dif"],
            GetRepositoryRoot());

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown command 'dif'", result.StandardError);
        Assert.Contains("Usage:", result.StandardError);
    }

    [Fact]
    public async Task HelpFlag_WritesUsageAndReturnsZeroExitCode()
    {
        await BuildCliAsync();

        var result = await RunProcessAsync(
            "dotnet",
            [GetCliAssemblyPath(), "--help"],
            GetRepositoryRoot());

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage:", result.StandardOutput);
    }

    [Fact]
    public async Task SampleSimplified_AppStartsViaAppHostAndDotnetRun()
    {
        var workspace = CreateSampleWorkspace("Sample.Simplified");

        try
        {
            var projectPath = Path.Combine(workspace, "Sample.Simplified.App", "Sample.Simplified.App.csproj");
            var outputDirectory = Path.Combine(workspace, "Sample.Simplified.App", "bin", "Debug", "net10.0");

            var buildResult = await RunProcessAsync(
                "dotnet",
                ["build", projectPath, "--nologo", "--verbosity", "quiet"],
                workspace);

            Assert.Equal(0, buildResult.ExitCode);
            Assert.DoesNotContain("error", buildResult.StandardError, StringComparison.OrdinalIgnoreCase);

            var runtimeConfigPath = Directory.GetFiles(outputDirectory, "*.runtimeconfig.json", SearchOption.TopDirectoryOnly).Single();
            var executableName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(runtimeConfigPath));
            var nativeHostPath = Path.Combine(
                outputDirectory,
                OperatingSystem.IsWindows() ? $"{executableName}.exe" : executableName);

            Assert.True(File.Exists(nativeHostPath), $"Expected a runnable app host at '{nativeHostPath}'.");

            var nativeHostResult = await RunProcessAsync(
                nativeHostPath,
                [],
                workspace);

            Assert.Equal(0, nativeHostResult.ExitCode);
            Assert.True(string.IsNullOrWhiteSpace(nativeHostResult.StandardError), nativeHostResult.StandardError);
            Assert.Equal(
                "Contoso Coffee placed 2 line(s) totaling 45.75. Ship via Air in 1 day(s). Approval CARD-0046.",
                NormalizeLineEndings(nativeHostResult.StandardOutput).Trim());

            var runResult = await RunProcessAsync(
                "dotnet",
                ["run", "--project", projectPath, "--no-launch-profile", "--no-build"],
                workspace);

            Assert.Equal(0, runResult.ExitCode);
            Assert.True(string.IsNullOrWhiteSpace(runResult.StandardError), runResult.StandardError);
            Assert.Equal(
                "Contoso Coffee placed 2 line(s) totaling 45.75. Ship via Air in 1 day(s). Approval CARD-0046.",
                NormalizeLineEndings(runResult.StandardOutput).Trim());
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task BaselineCommand_WritesSnapshotNextToSolutionAndPrintsConfirmation()
    {
        await BuildCliAsync();
        var baselines = await LoadBaselinesAsync();
        var sample = baselines.Samples.Single(static sample => sample.Name == "Sample.Simplified");
        var solutionPath = GetRepositoryPath("samples", "Sample.Simplified", "Sample.Simplified.sln");
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        var baselinePath = Path.Combine(solutionDirectory, ".simplicity-baseline.json");
        var originalContent = await ReadOptionalFileAsync(baselinePath);

        try
        {
            await File.WriteAllTextAsync(baselinePath, "{\"stale\":true}");

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "baseline", solutionPath],
                GetRepositoryRoot());

            Assert.Equal(0, result.ExitCode);
            AssertMissingConfigWarning(result.StandardError, solutionDirectory);
            Assert.True(File.Exists(baselinePath), $"Baseline file not found at {baselinePath}");

            var standardOutput = NormalizeLineEndings(result.StandardOutput).Trim();
            var snapshot = await LoadSnapshotAsync(baselinePath);
            Assert.Contains(snapshot.ToSummary(), standardOutput);
            Assert.Contains($"Baseline written to {baselinePath}", standardOutput);
            Assert.Equal(sample.TotalProjects, snapshot.TotalProjects);
            Assert.Equal(sample.TotalFiles, snapshot.TotalFiles);
            Assert.Equal(sample.PrimaryPathFileCount, snapshot.PrimaryPathFileCount);
            Assert.Equal(sample.AbstractionLayerCount, snapshot.AbstractionLayerCount);
            Assert.Equal(sample.ExternalDependencyCount, snapshot.ExternalDependencyCount);
            Assert.Equal(sample.UnusedDependencyCount, snapshot.UnusedDependencyCount);
            Assert.Equal(sample.InterfacesWithSingleImplementation, snapshot.InterfacesWithSingleImplementation);
            AssertWithinTolerance(sample.AverageMethodComplexity, snapshot.AverageMethodComplexity, 0.05d, sample.Name, nameof(snapshot.AverageMethodComplexity));
            AssertTimeSpanWithinTolerance(sample.EstimatedOnboardingTime, snapshot.EstimatedOnboardingTime, 0.10d, sample.Name);
            Assert.NotEqual(default, snapshot.CollectedAt);
        }
        finally
        {
            await RestoreOptionalFileAsync(baselinePath, originalContent);
            DeleteDirectoryIfExists(Path.Combine(solutionDirectory, ".simplicity-history"));
        }
    }

    [Fact]
    public async Task BaselineCommand_OverwritesExistingBaselineFile()
    {
        await BuildCliAsync();
        var solutionPath = GetRepositoryPath("samples", "Sample.OverEngineered", "Sample.OverEngineered.sln");
        var baselinePath = Path.Combine(Path.GetDirectoryName(solutionPath)!, ".simplicity-baseline.json");
        var originalContent = await ReadOptionalFileAsync(baselinePath);

        try
        {
            await File.WriteAllTextAsync(baselinePath, "stale baseline");

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "baseline", solutionPath],
                GetRepositoryRoot());

            Assert.Equal(0, result.ExitCode);

            var baselineContent = await File.ReadAllTextAsync(baselinePath);
            Assert.DoesNotContain("stale baseline", baselineContent);
            Assert.Contains("\"totalProjects\":", baselineContent);
            Assert.Contains("\"collectedAt\":", baselineContent);
        }
        finally
        {
            await RestoreOptionalFileAsync(baselinePath, originalContent);
            DeleteDirectoryIfExists(Path.Combine(Path.GetDirectoryName(solutionPath)!, ".simplicity-history"));
        }
    }

    [Fact]
    public async Task DiffCommand_FailsWithClearErrorWhenBaselineIsMissing()
    {
        await BuildCliAsync();
        var workspace = CreateSampleWorkspace("Sample.Simplified");

        try
        {
            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "diff", Path.Combine(workspace, "Sample.Simplified.sln")],
                workspace);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Baseline file was not found", result.StandardError);
            Assert.Contains("dotnet simplicity baseline", result.StandardError);
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task ReportCommand_WritesHistoryAndUnlocksTrendOnSecondRun()
    {
        await BuildCliAsync();
        var workspace = CreateSampleWorkspace("Sample.Simplified");

        try
        {
            var solutionPath = Path.Combine(workspace, "Sample.Simplified.sln");

            var firstRun = await RunProcessAsync("dotnet", [GetCliAssemblyPath(), "report", solutionPath], workspace);
            Assert.Equal(0, firstRun.ExitCode);
            Assert.Contains("Snapshot saved to", firstRun.StandardOutput);

            var historyDirectory = Path.Combine(workspace, ".simplicity-history");
            Assert.Single(Directory.GetFiles(historyDirectory, "*.json"));

            var secondRun = await RunProcessAsync("dotnet", [GetCliAssemblyPath(), "report", solutionPath], workspace);
            Assert.Equal(0, secondRun.ExitCode);
            Assert.Equal(2, Directory.GetFiles(historyDirectory, "*.json").Length);

            var htmlContent = await File.ReadAllTextAsync(Path.Combine(workspace, "simplicity-report", "index.html"));
            Assert.Contains("Trend Wave", htmlContent);
            Assert.DoesNotContain("acting as your teaching baseline", htmlContent);
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task DiffCommand_PrintsMetricAndFilterDeltasAgainstBaseline()
    {
        await BuildCliAsync();
        var solutionPath = GetRepositoryPath("samples", "Sample.Simplified", "Sample.Simplified.sln");
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        var baselinePath = Path.Combine(solutionDirectory, ".simplicity-baseline.json");
        var originalContent = await ReadOptionalFileAsync(baselinePath);

        try
        {
            var collector = new SimplicityCollector();
            var currentSnapshot = await collector.CollectAsync(solutionPath);
            var baselineSnapshot = currentSnapshot with
            {
                TotalFiles = currentSnapshot.TotalFiles - 3,
                CollectedAt = new DateTimeOffset(2026, 04, 01, 0, 0, 0, TimeSpan.Zero)
            };

            await BaselineSnapshotFile.WriteAsync(solutionPath, baselineSnapshot);

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "diff", solutionPath],
                GetRepositoryRoot());

            Assert.Equal(0, result.ExitCode);
            AssertMissingConfigWarning(result.StandardError, solutionDirectory);

            var output = NormalizeLineEndings(result.StandardOutput).Trim();
            var baselinePrimaryPathScore = PrimaryPathFirstEvaluator.Evaluate(baselineSnapshot).Score;
            var currentPrimaryPathScore = PrimaryPathFirstEvaluator.Evaluate(currentSnapshot).Score;

            Assert.Contains("Simplicity Diff", output);
            Assert.Contains($"Baseline file: {baselinePath}", output);
            Assert.Contains($"- Total files: {baselineSnapshot.TotalFiles} -> {currentSnapshot.TotalFiles} (+3)", output);
            Assert.Contains(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"- {FilterName.PrimaryPathFirst}: {baselinePrimaryPathScore:F2} -> {currentPrimaryPathScore:F2} ({currentPrimaryPathScore - baselinePrimaryPathScore:F2})"),
                output);
            Assert.Contains("Regression status: no regressions detected.", output);
        }
        finally
        {
            await RestoreOptionalFileAsync(baselinePath, originalContent);
        }
    }

    [Fact]
    public async Task DiffCommand_FailsOnRegressionWhenRequested()
    {
        await BuildCliAsync();
        var solutionPath = GetRepositoryPath("samples", "Sample.Simplified", "Sample.Simplified.sln");
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        var baselinePath = Path.Combine(solutionDirectory, ".simplicity-baseline.json");
        var originalContent = await ReadOptionalFileAsync(baselinePath);

        try
        {
            var collector = new SimplicityCollector();
            var currentSnapshot = await collector.CollectAsync(solutionPath);
            var baselineSnapshot = currentSnapshot with
            {
                TotalFiles = currentSnapshot.PrimaryPathFileCount,
                AverageMethodComplexity = currentSnapshot.AverageMethodComplexity - 0.6d,
                CollectedAt = new DateTimeOffset(2026, 04, 01, 0, 0, 0, TimeSpan.Zero)
            };

            await BaselineSnapshotFile.WriteAsync(solutionPath, baselineSnapshot);

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "diff", solutionPath, "--fail-on-regression"],
                GetRepositoryRoot());

            Assert.Equal(1, result.ExitCode);
            AssertMissingConfigWarning(result.StandardError, solutionDirectory);

            var output = NormalizeLineEndings(result.StandardOutput).Trim();
            var primaryPathScoreDelta = PrimaryPathFirstEvaluator.Evaluate(currentSnapshot).Score - PrimaryPathFirstEvaluator.Evaluate(baselineSnapshot).Score;
            var complexityDelta = currentSnapshot.AverageMethodComplexity - baselineSnapshot.AverageMethodComplexity;

            Assert.Contains("Regression status: 2 regression(s) detected.", output);
            Assert.Contains(
                $"AverageMethodComplexity increased by {complexityDelta.ToString("F2", CultureInfo.InvariantCulture)} (threshold +0.50).",
                output);
            Assert.Contains(
                $"PrimaryPathFirst score decreased by {Math.Abs(primaryPathScoreDelta).ToString("F2", CultureInfo.InvariantCulture)} (threshold -0.10).",
                output);
        }
        finally
        {
            await RestoreOptionalFileAsync(baselinePath, originalContent);
        }
    }

    [Fact]
    public async Task BudgetCommand_PrintsAllFourDimensionsAgainstDefaultThresholds()
    {
        await BuildCliAsync();
        var solutionPath = GetRepositoryPath("samples", "Sample.Simplified", "Sample.Simplified.sln");
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;

        var result = await RunProcessAsync(
            "dotnet",
            [GetCliAssemblyPath(), "budget", solutionPath],
            GetRepositoryRoot());

        Assert.Equal(0, result.ExitCode);
        AssertMissingConfigWarning(result.StandardError, solutionDirectory);

        var output = NormalizeLineEndings(result.StandardOutput).Trim();

        Assert.Contains("Complexity Budget", output);
        Assert.Contains("Status:", output);
        Assert.Contains("Cognitive Load", output);
        Assert.Contains("Operational Surface", output);
        Assert.Contains("Change Safety", output);
        Assert.Contains("Discoverability", output);
        Assert.Contains("Cognitive Load      not scored — onboarding time has not been computed.", output);
        Assert.Contains("Premature abstraction ratio:", output);
        Assert.Contains("target <= 0.25", output);
        Assert.Contains("Average method complexity:", output);
        Assert.Contains("target <= 5.00", output);
        Assert.Contains("Primary path ratio:", output);
        Assert.Contains("target >= 0.60", output);
    }

    [Fact]
    public async Task BudgetCommand_AcceptsSchemaPointerInSimplicityJson()
    {
        await BuildCliAsync();
        var workspace = CreateSampleWorkspace("Sample.Simplified");

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(workspace, "simplicity.json"),
                """
                {
                  "$schema": "https://github.com/cwoodruff/SimplicityTools/blob/main/docs/simplicity-schema.json",
                  "filters": {
                    "maxMethodComplexity": 1.5
                  }
                }
                """);

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "budget", Path.Combine(workspace, "Sample.Simplified.sln")],
                GetRepositoryRoot());

            Assert.Equal(0, result.ExitCode);
            Assert.True(string.IsNullOrWhiteSpace(result.StandardError), result.StandardError);

            // The pointer is ignored, and the sibling threshold override still applies.
            Assert.Contains("target <= 1.50", NormalizeLineEndings(result.StandardOutput));
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task BudgetCommand_UsesThresholdOverridesFromSimplicityJson()
    {
        await BuildCliAsync();
        var workspace = CreateSampleWorkspace("Sample.Simplified");

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(workspace, "simplicity.json"),
                """
                {
                  "filters": {
                    "primaryPathRatioTarget": 0.95,
                    "prematureAbstractionRatioTarget": 0.05,
                    "maxMethodComplexity": 1.5,
                    "maxOnboardingHours": 10
                  }
                }
                """);

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "budget", Path.Combine(workspace, "Sample.Simplified.sln")],
                GetRepositoryRoot());

            Assert.Equal(0, result.ExitCode);
            Assert.True(string.IsNullOrWhiteSpace(result.StandardError), result.StandardError);

            var output = NormalizeLineEndings(result.StandardOutput).Trim();

            Assert.Contains("target <= 0.05", output);
            Assert.Contains("target <= 1.50", output);
            Assert.Contains("target >= 0.95", output);
            Assert.Contains("OVER BUDGET", output);
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task WatchCommandRunner_AppliesConfiguredFilterThresholds()
    {
        var workspace = CreateSampleWorkspace("Sample.Simplified");
        try
        {
            var solutionPath = Path.Combine(workspace, "Sample.Simplified.sln");
            await File.WriteAllTextAsync(
                Path.Combine(workspace, "simplicity.json"),
                """{ "filters": { "passingScore": 0.99 } }""");

            using var outputWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var runner = new WatchCommandRunner(
                solutionPath,
                outputWriter,
                errorWriter,
                debounceDelay: TimeSpan.FromMilliseconds(150));

            var runTask = runner.RunAsync(cancellationTokenSource.Token);
            try
            {
                await WaitForConditionAsync(
                    () => NormalizeLineEndings(outputWriter.ToString()).Contains("Filter Verdicts", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(15));
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }

            await runTask;

            var output = NormalizeLineEndings(outputWriter.ToString());
            // Sample.Simplified passes every filter at the default 0.70, but its primary-path
            // concentration cannot reach a 0.99 passing score.
            Assert.Contains("PrimaryPathFirst: FAIL", output);
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task WatchCommandRunner_ReanalyzesAfterFileChangesAndPrintsFilterVerdicts()
    {
        var workspace = CreateSampleWorkspace("Sample.Simplified");
        try
        {
            var solutionPath = Path.Combine(workspace, "Sample.Simplified.sln");
            var watchedFile = Directory
                .GetFiles(workspace, "*.cs", SearchOption.AllDirectories)
                .First(static path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                                       !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
            using var outputWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var runner = new WatchCommandRunner(
                solutionPath,
                outputWriter,
                errorWriter,
                debounceDelay: TimeSpan.FromMilliseconds(150));

            var runTask = runner.RunAsync(cancellationTokenSource.Token);
            try
            {
                await WaitForConditionAsync(
                    () => NormalizeLineEndings(outputWriter.ToString()).Contains("Initial snapshot", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(10));

                await File.AppendAllTextAsync(watchedFile, $"{Environment.NewLine}// watch test pulse");

                await WaitForConditionAsync(
                    () => NormalizeLineEndings(outputWriter.ToString()).Contains("Updated snapshot", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(10));

                await Task.Delay(1000);
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }

            var exitCode = await runTask;
            Assert.Equal(0, exitCode);

            var output = NormalizeLineEndings(outputWriter.ToString());
            var error = NormalizeLineEndings(errorWriter.ToString());

            Assert.Contains($"Watching {solutionPath}", output);
            Assert.Contains("Press Ctrl+C to stop.", output);
            Assert.Contains("Initial snapshot", output);
            Assert.Contains("Updated snapshot", output);
            Assert.Contains("Change detected:", output);
            Assert.Contains("Filter Verdicts", output);
            Assert.Contains("TwoAmTest", output);
            Assert.Contains("HalfRule", output);
            Assert.Contains("PrimaryPathFirst", output);
            Assert.Equal(1, CountOccurrences(output, "Updated snapshot"));
            Assert.Equal(1, CountOccurrences(error, "Info: using built-in defaults (no simplicity.json found"));
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task WatchChangeDebouncer_CollapsesBurstIntoSingleUpdate()
    {
        var notifications = new List<WatchChangeNotification>();
        var callbackCompletion = new TaskCompletionSource<WatchChangeNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var shutdownSource = new CancellationTokenSource();
        using var debouncer = new WatchChangeDebouncer(
            TimeSpan.FromMilliseconds(75),
            notification =>
            {
                notifications.Add(notification);
                callbackCompletion.TrySetResult(notification);
                return Task.CompletedTask;
            },
            shutdownSource.Token);

        debouncer.Signal(new WatchChangeNotification(WatcherChangeTypes.Changed, "/repo/first.cs", PreviousFullPath: null));
        await Task.Delay(25);
        debouncer.Signal(new WatchChangeNotification(WatcherChangeTypes.Changed, "/repo/second.cs", PreviousFullPath: null));

        var notification = await callbackCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(150);

        Assert.Single(notifications);
        Assert.Equal("second.cs", Path.GetFileName(notification.FullPath));
    }

    [Fact]
    public async Task ReportCommand_GeneratesSelfContainedHtmlForBothSamples()
    {
        await BuildCliAsync();
        var baselines = await LoadBaselinesAsync();

        foreach (var sample in baselines.Samples)
        {
            var workspace = CreateWorkspace();

            try
            {
                var result = await RunProcessAsync(
                    "dotnet",
                    [GetCliAssemblyPath(), "report", GetRepositoryPath(sample.SolutionPath)],
                    workspace);

                Assert.Equal(0, result.ExitCode);
                AssertMissingConfigWarning(result.StandardError, Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!);

                // The report now defaults to <solution-directory>/simplicity-report, not the CWD.
                var reportPath = Path.Combine(Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!, "simplicity-report", "index.html");
                Assert.True(File.Exists(reportPath), $"Report file not found at {reportPath}");
                Assert.Contains($"Report generated to {reportPath}", result.StandardOutput);

                var htmlContent = await File.ReadAllTextAsync(reportPath);

                Assert.Contains("<!DOCTYPE html>", htmlContent);
                Assert.Contains("<html lang=\"en\">", htmlContent);
                Assert.Contains("Simplicity Report", htmlContent);
                Assert.Contains("#0D0D0D", htmlContent);
                Assert.Contains("#E31B23", htmlContent);
                Assert.Contains("Executive Summary", htmlContent);
                Assert.Contains("Filter Verdicts", htmlContent);
                Assert.Contains("Metric Detail", htmlContent);
                Assert.Contains("Complexity Budget", htmlContent);
                Assert.Contains("Trend Analysis", htmlContent);
                Assert.Contains("Trend analysis unlocks when <code>.simplicity-history/</code> contains at least two snapshots.", htmlContent);
                Assert.Contains("Appendix", htmlContent);

                Assert.DoesNotContain("http://", htmlContent);
                Assert.DoesNotContain("https://", htmlContent);
                Assert.DoesNotContain("<link rel=\"stylesheet\"", htmlContent);
                Assert.DoesNotContain("<script src=\"", htmlContent);
            }
            finally
            {
                DeleteDirectoryIfExists(workspace);
                DeleteDirectoryIfExists(Path.Combine(Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!, ".simplicity-history"));
                DeleteDirectoryIfExists(Path.Combine(Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!, "simplicity-report"));
            }
        }
    }

    [Fact]
    public async Task ReportCommand_IncludesSnapshotMetricsInHtml()
    {
        await BuildCliAsync();
        var baselines = await LoadBaselinesAsync();
        var sample = baselines.Samples.First();

        var workspace = CreateWorkspace();

        try
        {
            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "report", GetRepositoryPath(sample.SolutionPath)],
                workspace);

            Assert.Equal(0, result.ExitCode);
            AssertMissingConfigWarning(result.StandardError, Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!);

            var reportPath = Path.Combine(Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!, "simplicity-report", "index.html");
            var htmlContent = await File.ReadAllTextAsync(reportPath);

            Assert.Contains(sample.TotalProjects.ToString(CultureInfo.InvariantCulture), htmlContent);
            Assert.Contains(sample.TotalFiles.ToString(CultureInfo.InvariantCulture), htmlContent);
            Assert.Contains(sample.PrimaryPathFileCount.ToString(CultureInfo.InvariantCulture), htmlContent);
            Assert.Contains(sample.AbstractionLayerCount.ToString(CultureInfo.InvariantCulture), htmlContent);
            Assert.Contains(sample.ExternalDependencyCount.ToString(CultureInfo.InvariantCulture), htmlContent);
            Assert.Contains(sample.UnusedDependencyCount.ToString(CultureInfo.InvariantCulture), htmlContent);
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
            DeleteDirectoryIfExists(Path.Combine(Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!, ".simplicity-history"));
            DeleteDirectoryIfExists(Path.Combine(Path.GetDirectoryName(GetRepositoryPath(sample.SolutionPath))!, "simplicity-report"));
        }
    }

    [Fact]
    public async Task ReportCommand_RendersTrendWaveWhenHistoryContainsMultipleSnapshots()
    {
        await BuildCliAsync();
        var workspace = CreateSampleWorkspace("Sample.Simplified");

        try
        {
            var solutionPath = Path.Combine(workspace, "Sample.Simplified.sln");
            var historyDirectory = Path.Combine(workspace, ".simplicity-history");
            Directory.CreateDirectory(historyDirectory);

            await WriteSnapshotAsync(
                Path.Combine(historyDirectory, "2026-04-28T090000Z.json"),
                new SimplicitySnapshot
                {
                    TotalProjects = 2,
                    TotalFiles = 23,
                    PrimaryPathFileCount = 4,
                    AbstractionLayerCount = 3,
                    ExternalDependencyCount = 0,
                    UnusedDependencyCount = 1,
                    InterfacesWithSingleImplementation = 2,
                    AverageMethodComplexity = 2.40d,
                    EstimatedOnboardingTime = TimeSpan.FromHours(18),
                    CollectedAt = DateTimeOffset.Parse("2026-04-28T09:00:00Z", CultureInfo.InvariantCulture)
                });

            await WriteSnapshotAsync(
                Path.Combine(historyDirectory, "2026-04-29T090000Z.json"),
                new SimplicitySnapshot
                {
                    TotalProjects = 2,
                    TotalFiles = 23,
                    PrimaryPathFileCount = 5,
                    AbstractionLayerCount = 2,
                    ExternalDependencyCount = 0,
                    UnusedDependencyCount = 0,
                    InterfacesWithSingleImplementation = 1,
                    AverageMethodComplexity = 2.10d,
                    EstimatedOnboardingTime = TimeSpan.FromHours(14),
                    CollectedAt = DateTimeOffset.Parse("2026-04-29T09:00:00Z", CultureInfo.InvariantCulture)
                });

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "report", solutionPath],
                workspace);

            Assert.Equal(0, result.ExitCode);
            AssertMissingConfigWarning(result.StandardError, workspace);

            var reportPath = Path.Combine(workspace, "simplicity-report", "index.html");
            var htmlContent = await File.ReadAllTextAsync(reportPath);

            Assert.Contains("<svg class=\"trend-chart\"", htmlContent);
            Assert.Contains("Trend Wave", htmlContent);
            Assert.Contains("Historical Filter Scores", htmlContent);
            Assert.Contains("Complexity Deltas", htmlContent);
            Assert.Contains("Current report", htmlContent);
            Assert.Contains("Apr 28, 2026", htmlContent);
            Assert.Contains("Apr 29, 2026", htmlContent);
            Assert.DoesNotContain("Trend analysis unlocks when <code>.simplicity-history/</code> contains at least two snapshots.", htmlContent);
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public void ConfigurationLoader_UsesDefaultsAndWarningWhenFileIsMissing()
    {
        var solutionPath = GetRepositoryPath("samples", "Sample.Simplified", "Sample.Simplified.sln");
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        var configuration = SimplicityConfigurationLoader.LoadForSolution(solutionPath, writer);

        Assert.Equal(SimplicityConfiguration.Defaults, configuration);
        AssertMissingConfigWarning(writer.ToString(), Path.GetDirectoryName(solutionPath)!);
    }

    [Fact]
    public void ConfigurationLoader_MergesPartialOverridesWithDefaults()
    {
        var workspace = CreateWorkspace();

        try
        {
            var solutionPath = CreatePlaceholderSolution(workspace);
            File.WriteAllText(
                Path.Combine(workspace, "simplicity.json"),
                """
                {
                  "tca": {
                    "teamSize": 12
                  },
                  "filters": {
                    "passingScore": 0.85
                  }
                }
                """);

            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            var configuration = SimplicityConfigurationLoader.LoadForSolution(solutionPath, writer);

            Assert.Equal(12, configuration.Tca.TeamSize);
            Assert.Equal(SimplicityConfiguration.Defaults.Tca.AverageEngineerMonthlySalaryUsd, configuration.Tca.AverageEngineerMonthlySalaryUsd);
            Assert.Equal(0.85d, configuration.Filters.PassingScore);
            Assert.Equal(SimplicityConfiguration.Defaults.Filters.MaxMethodComplexity, configuration.Filters.MaxMethodComplexity);
            Assert.Equal(string.Empty, writer.ToString());
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task AnalyzeCommand_FailsFastForInvalidConfiguration()
    {
        await BuildCliAsync();
        var workspace = CreateSampleWorkspace("Sample.Simplified");

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(workspace, "simplicity.json"),
                """
                {
                  "filters": {
                    "passingScore": 1.5
                  }
                }
                """);

            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "analyze", Path.Combine(workspace, "Sample.Simplified.sln")],
                GetRepositoryRoot());

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Invalid simplicity.json", result.StandardError);
            Assert.Contains("$.filters.passingScore must be less than or equal to 1", result.StandardError);
            Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), result.StandardOutput);
        }
        finally
        {
            DeleteDirectoryIfExists(workspace);
        }
    }

    private static async Task<SampleBaselineSet> LoadBaselinesAsync()
    {
        await using var stream = File.OpenRead(GetRepositoryPath("tests", "SimplicitySampleBaselines.json"));
        return await JsonSerializer.DeserializeAsync<SampleBaselineSet>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Could not load sample baselines.");
    }

    private static async Task<SimplicitySnapshot> LoadSnapshotAsync(string baselinePath)
    {
        var json = await File.ReadAllTextAsync(baselinePath);
        return SnapshotEnvelope.Deserialize(json, $"Baseline file '{baselinePath}'");
    }

    private static async Task<string?> ReadOptionalFileAsync(string path)
    {
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path)
            : null;
    }

    private static async Task WriteSnapshotAsync(string path, SimplicitySnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(path, json);
    }

    private static async Task RestoreOptionalFileAsync(string path, string? originalContent)
    {
        if (originalContent is null)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        await File.WriteAllTextAsync(path, originalContent);
    }

    private static async Task BuildCliAsync()
    {
        if (cliBuilt)
        {
            return;
        }

        await BuildLock.WaitAsync();

        try
        {
            if (cliBuilt)
            {
                return;
            }

            var result = await RunProcessAsync(
                "dotnet",
                ["build", GetRepositoryPath("src", "SimplicityTools.Cli", "SimplicityTools.Cli.csproj"), "--no-restore", "--nologo", "--verbosity", "quiet"],
                GetRepositoryRoot());

            Assert.True(result.ExitCode == 0, $"dotnet build failed (exit {result.ExitCode}):\nstdout: {result.StandardOutput}\nstderr: {result.StandardError}");
            cliBuilt = true;
        }
        finally
        {
            BuildLock.Release();
        }
    }

    private static string GetCliAssemblyPath()
    {
        return GetRepositoryPath("src", "SimplicityTools.Cli", "bin", "Debug", "net10.0", "SimplicityTools.Cli.dll");
    }

    private static DateTimeOffset ParseSummaryDate(string summary)
    {
        var match = Regex.Match(summary, @"^Simplicity Snapshot \((?<date>\d{4}-\d{2}-\d{2})\)", RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            throw new Xunit.Sdk.XunitException($"Could not parse summary date from output:{Environment.NewLine}{summary}");
        }

        return DateTimeOffset.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        using var process = new Process
        {
            StartInfo =
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start process '{fileName}'.");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }

    private static void AssertWithinTolerance(double expected, double actual, double tolerance, string sampleName, string metricName)
    {
        if (expected == 0d)
        {
            Assert.Equal(expected, actual);
            return;
        }

        var delta = Math.Abs(actual - expected) / expected;
        Assert.True(delta <= tolerance, $"{sampleName} {metricName} expected {expected}, actual {actual}, tolerance {tolerance:P0}.");
    }

    private static void AssertTimeSpanWithinTolerance(TimeSpan? expected, TimeSpan? actual, double tolerance, string sampleName)
    {
        if (expected is not { } expectedTime || expectedTime == TimeSpan.Zero)
        {
            Assert.Equal(expected is null || expected == TimeSpan.Zero, actual is null || actual == TimeSpan.Zero);
            return;
        }

        Assert.NotNull(actual);
        var delta = Math.Abs((actual.Value - expectedTime).TotalSeconds) / expectedTime.TotalSeconds;
        Assert.True(delta <= tolerance, $"{sampleName} EstimatedOnboardingTime expected {expected}, actual {actual}, tolerance {tolerance:P0}.");
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string substring)
    {
        var count = 0;
        var currentIndex = 0;

        while ((currentIndex = value.IndexOf(substring, currentIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            currentIndex += substring.Length;
        }

        return count;
    }

    private static void AssertMissingConfigWarning(string standardError, string expectedDirectory)
    {
        var normalized = NormalizeLineEndings(standardError).Trim();
        Assert.Equal($"Info: using built-in defaults (no simplicity.json found in '{expectedDirectory}').", normalized);
    }

    private static string CreateSampleWorkspace(string sampleDirectoryName)
    {
        var sourceDirectory = GetRepositoryPath("samples", sampleDirectoryName);
        var workspace = CreateWorkspace();
        CopyDirectory(sourceDirectory, workspace);
        return workspace;
    }

    private static string CreatePlaceholderSolution(string workspace)
    {
        var solutionPath = Path.Combine(workspace, "placeholder.sln");
        File.WriteAllText(solutionPath, "Microsoft Visual Studio Solution File, Format Version 12.00");
        return solutionPath;
    }

    private static string CreateWorkspace()
    {
        var workspace = GetRepositoryPath("tests", "SimplicityTools.Cli.Tests", ".workspace", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new Xunit.Sdk.XunitException($"Condition was not satisfied within {timeout}.");
    }

    private static string GetRepositoryPath(params string[] segments)
    {
        return Path.Combine([GetRepositoryRoot(), .. segments]);
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

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    public sealed record SampleBaselineSet(IReadOnlyList<SampleBaseline> Samples);

    public sealed record SampleBaseline(
        string Name,
        string SolutionPath,
        int TotalProjects,
        int TotalFiles,
        int PrimaryPathFileCount,
        int AbstractionLayerCount,
        int ExternalDependencyCount,
        int UnusedDependencyCount,
        int InterfacesWithSingleImplementation,
        double AverageMethodComplexity,
        TimeSpan? EstimatedOnboardingTime)
    {
        public SimplicitySnapshot ToSnapshot(DateTimeOffset collectedAt)
        {
            return new SimplicitySnapshot
            {
                TotalProjects = TotalProjects,
                TotalFiles = TotalFiles,
                PrimaryPathFileCount = PrimaryPathFileCount,
                AbstractionLayerCount = AbstractionLayerCount,
                ExternalDependencyCount = ExternalDependencyCount,
                UnusedDependencyCount = UnusedDependencyCount,
                InterfacesWithSingleImplementation = InterfacesWithSingleImplementation,
                AverageMethodComplexity = AverageMethodComplexity,
                EstimatedOnboardingTime = EstimatedOnboardingTime,
                CollectedAt = collectedAt
            };
        }
    }
}
