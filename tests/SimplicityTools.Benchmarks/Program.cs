using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Diagnostics;

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, AnalyzeBenchmarkConfig.Instance);

internal sealed class AnalyzeBenchmarkConfig : ManualConfig
{
    public static AnalyzeBenchmarkConfig Instance { get; } = new();

    private AnalyzeBenchmarkConfig()
    {
        AddJob(Job.ShortRun.WithWarmupCount(3).WithIterationCount(10));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddLogger(ConsoleLogger.Unicode);
        AddExporter(MarkdownExporter.Default);
        AddExporter(JsonExporter.Full);
        AddColumn(StatisticColumn.P95);
        AddColumnProvider(DefaultColumnProviders.Instance);
    }
}

[MemoryDiagnoser]
public class AnalyzeCommandBenchmarks
{
    private string cliAssemblyPath = string.Empty;
    private string repositoryRoot = string.Empty;
    private string overEngineeredSolutionPath = string.Empty;

    [GlobalSetup]
    public void GlobalSetup()
    {
        repositoryRoot = GetRepositoryRoot();
        overEngineeredSolutionPath = Path.Combine(repositoryRoot, "samples", "Sample.OverEngineered", "Sample.OverEngineered.sln");
        cliAssemblyPath = Path.Combine(repositoryRoot, "src", "SimplicityTools.Cli", "bin", "Release", "net10.0", "SimplicityTools.Cli.dll");

        var build = RunProcess(
            "dotnet",
            ["build", Path.Combine(repositoryRoot, "src", "SimplicityTools.Cli", "SimplicityTools.Cli.csproj"), "-c", "Release", "--nologo", "--verbosity", "quiet"],
            repositoryRoot);

        if (build.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to build CLI benchmark target:{Environment.NewLine}{build.StandardError}");
        }
    }

    [Benchmark(Description = "CLI analyze Sample.OverEngineered")]
    public int AnalyzeOverEngineered()
    {
        var result = RunProcess(
            "dotnet",
            [cliAssemblyPath, "analyze", overEngineeredSolutionPath],
            repositoryRoot);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Analyze command failed:{Environment.NewLine}{result.StandardError}");
        }

        return result.ExitCode;
    }

    private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
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

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SimplicityTools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root from the benchmark base directory.");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
