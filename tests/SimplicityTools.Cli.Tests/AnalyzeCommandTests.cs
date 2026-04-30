using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class AnalyzeCommandTests
{
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
            Assert.True(string.IsNullOrWhiteSpace(result.StandardError), result.StandardError);

            var actual = NormalizeLineEndings(result.StandardOutput).Trim();
            var summaryDate = ParseSummaryDate(actual);
            var expected = sample.ToSnapshot(summaryDate).ToSummary();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public async Task ReportCommand_GeneratesSelfContainedHtmlForBothSamples()
    {
        await BuildCliAsync();
        var baselines = await LoadBaselinesAsync();

        foreach (var sample in baselines.Samples)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"simplicity-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var result = await RunProcessAsync(
                    "dotnet",
                    [GetCliAssemblyPath(), "report", GetRepositoryPath(sample.SolutionPath)],
                    tempDir);

                Assert.Equal(0, result.ExitCode);
                Assert.True(string.IsNullOrWhiteSpace(result.StandardError), result.StandardError);

                var reportPath = Path.Combine(tempDir, "simplicity-report", "index.html");
                Assert.True(File.Exists(reportPath), $"Report file not found at {reportPath}");

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
                Assert.Contains("Appendix", htmlContent);

                Assert.DoesNotContain("http://", htmlContent);
                Assert.DoesNotContain("https://", htmlContent);
                Assert.DoesNotContain("<link rel=\"stylesheet\"", htmlContent);
                Assert.DoesNotContain("<script src=\"", htmlContent);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                }
            }
        }
    }

    [Fact]
    public async Task ReportCommand_IncludesSnapshotMetricsInHtml()
    {
        await BuildCliAsync();
        var baselines = await LoadBaselinesAsync();
        var sample = baselines.Samples.First();

        var tempDir = Path.Combine(Path.GetTempPath(), $"simplicity-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = await RunProcessAsync(
                "dotnet",
                [GetCliAssemblyPath(), "report", GetRepositoryPath(sample.SolutionPath)],
                tempDir);

            Assert.Equal(0, result.ExitCode);

            var reportPath = Path.Combine(tempDir, "simplicity-report", "index.html");
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
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
            }
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

    private static async Task BuildCliAsync()
    {
        var result = await RunProcessAsync(
            "dotnet",
            ["build", GetRepositoryPath("src", "SimplicityTools.Cli", "SimplicityTools.Cli.csproj"), "--nologo", "--verbosity", "quiet"],
            GetRepositoryRoot());

        Assert.Equal(0, result.ExitCode);
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

    private static void AssertTimeSpanWithinTolerance(TimeSpan expected, TimeSpan actual, double tolerance, string sampleName)
    {
        if (expected == TimeSpan.Zero)
        {
            Assert.Equal(expected, actual);
            return;
        }

        var delta = Math.Abs((actual - expected).TotalSeconds) / expected.TotalSeconds;
        Assert.True(delta <= tolerance, $"{sampleName} EstimatedOnboardingTime expected {expected}, actual {actual}, tolerance {tolerance:P0}.");
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
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
        TimeSpan EstimatedOnboardingTime)
    {
        public SimplicitySnapshot ToSnapshot(DateTimeOffset collectedAt)
        {
            return new SimplicitySnapshot(
                TotalProjects,
                TotalFiles,
                PrimaryPathFileCount,
                AbstractionLayerCount,
                ExternalDependencyCount,
                UnusedDependencyCount,
                InterfacesWithSingleImplementation,
                AverageMethodComplexity,
                EstimatedOnboardingTime,
                collectedAt);
        }
    }
}
