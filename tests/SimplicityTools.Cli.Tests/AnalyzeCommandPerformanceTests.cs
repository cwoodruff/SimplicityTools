using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class AnalyzeCommandPerformanceTests
{
    private const string GitHubActionsEnvironmentVariable = "GITHUB_ACTIONS";
    private readonly Xunit.Abstractions.ITestOutputHelper output;

    public AnalyzeCommandPerformanceTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        this.output = output;
    }

    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static bool cliBuilt;

    [Fact]
    public async Task AnalyzeCommand_OverEngineeredSample_CompletesWithinExpectedThresholdAtP95()
    {
        await BuildCliAsync();

        var solutionPath = GetRepositoryPath("samples", "Sample.OverEngineered", "Sample.OverEngineered.sln");
        const int warmupRuns = 2;
        const int measuredRuns = 15;

        for (var index = 0; index < warmupRuns; index++)
        {
            var warmup = await MeasureAnalyzeAsync(solutionPath);
            Assert.Equal(0, warmup.ExitCode);
        }

        var durations = new List<TimeSpan>(measuredRuns);

        for (var index = 0; index < measuredRuns; index++)
        {
            var measurement = await MeasureAnalyzeAsync(solutionPath);
            Assert.Equal(0, measurement.ExitCode);
            durations.Add(measurement.Duration);
        }

        var p95 = CalculatePercentile(durations, 0.95d);
        var threshold = GetP95Threshold();

        // Emitted on success too, so threshold headroom is visible in CI logs before drift
        // ever turns into a failure (issue #104: the old 10s threshold sat inside runner noise
        // and the first sign was a coin-flip red).
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"analyze p95 {p95.TotalSeconds:F3}s (threshold {threshold.TotalSeconds:F0}s, {GetPerformanceEnvironmentLabel()}); runs: {string.Join(", ", durations.Select(static duration => duration.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)))}"));

        Assert.True(
            p95 < threshold,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Expected analyze p95 under {threshold.TotalSeconds:F0}s for Sample.OverEngineered in {GetPerformanceEnvironmentLabel()}, but observed {p95.TotalSeconds:F3}s. Runs: {string.Join(", ", durations.Select(static duration => duration.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)))}"));
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
                ["build", GetRepositoryPath("src", "SimplicityTools.Cli", "SimplicityTools.Cli.csproj"), "-c", "Release", "--no-restore", "--nologo", "--verbosity", "quiet"],
                GetRepositoryRoot());

            Assert.True(result.ExitCode == 0, $"dotnet build failed (exit {result.ExitCode}):\nstdout: {result.StandardOutput}\nstderr: {result.StandardError}");
            cliBuilt = true;
        }
        finally
        {
            BuildLock.Release();
        }
    }

    private static async Task<PerformanceResult> MeasureAnalyzeAsync(string solutionPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await RunProcessAsync(
            "dotnet",
            [GetCliAssemblyPath(), "analyze", solutionPath],
            GetRepositoryRoot());
        stopwatch.Stop();

        return new PerformanceResult(result.ExitCode, stopwatch.Elapsed);
    }

    private static TimeSpan CalculatePercentile(IReadOnlyList<TimeSpan> samples, double percentile)
    {
        var ordered = samples.OrderBy(static sample => sample).ToArray();
        if (ordered.Length == 0)
        {
            throw new InvalidOperationException("At least one sample is required to calculate a percentile.");
        }

        if (ordered.Length == 1)
        {
            return ordered[0];
        }

        var rank = percentile * (ordered.Length - 1);
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);
        if (lowerIndex == upperIndex)
        {
            return ordered[lowerIndex];
        }

        var fraction = rank - lowerIndex;
        var interpolatedTicks = ordered[lowerIndex].Ticks + ((ordered[upperIndex].Ticks - ordered[lowerIndex].Ticks) * fraction);
        return TimeSpan.FromTicks(Convert.ToInt64(Math.Round(interpolatedTicks, MidpointRounding.AwayFromZero)));
    }

    private static TimeSpan GetP95Threshold()
    {
        // Absolute thresholds sized at roughly 3x the P95 measured after the single-pass
        // inbound-reference rework (#92): ~5s on ubuntu-latest runners, ~1.5-2s locally.
        // Anything inside runner noise produces coin-flip results (issue #104); anything much
        // looser stops catching real regressions like the pre-#92 quadratic pass.
        return IsRunningInGitHubActions()
            ? TimeSpan.FromSeconds(15)
            : TimeSpan.FromSeconds(5);
    }

    private static string GetPerformanceEnvironmentLabel()
    {
        return IsRunningInGitHubActions()
            ? "GitHub Actions CI"
            : "local/default environment";
    }

    private static bool IsRunningInGitHubActions()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable(GitHubActionsEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCliAssemblyPath()
    {
        return GetRepositoryPath("src", "SimplicityTools.Cli", "bin", "Release", "net10.0", "SimplicityTools.Cli.dll");
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

    private sealed record PerformanceResult(int ExitCode, TimeSpan Duration);
}
