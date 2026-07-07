using SimplicityTools.Cli;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

return await CommandLineEntryPoint.RunAsync(args);

internal static class CommandLineEntryPoint
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0 || IsHelpToken(args[0]))
            {
                WriteUsage(Console.Out);
                return 0;
            }

            if (Commands.TryGetValue(args[0], out var command))
            {
                return await command(args[1..]).ConfigureAwait(false);
            }

            Console.Error.WriteLine($"Unknown command '{args[0]}'.");
            WriteUsage(Console.Error);
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static readonly Dictionary<string, Func<string[], Task<int>>> Commands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["analyze"] = RunAnalyzeAsync,
            ["report"] = RunReportAsync,
            ["baseline"] = RunBaselineAsync,
            ["diff"] = RunDiffAsync,
            ["budget"] = RunBudgetAsync,
            ["watch"] = RunWatchAsync
        };

    private static bool IsHelpToken(string arg)
    {
        return string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(arg, "-h", StringComparison.Ordinal) ||
               string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase);
    }

    private static Task<int> RunAnalyzeAsync(string[] args)
    {
        return RunWithSnapshotAsync(args, static (snapshot, _, _) =>
        {
            Console.WriteLine(snapshot.ToSummary());
            return Task.FromResult(0);
        });
    }

    private static Task<int> RunReportAsync(string[] args)
    {
        return RunWithSnapshotAsync(args, static async (snapshot, configuration, solutionPath) =>
        {
            await SnapshotHistory.AppendAsync(solutionPath, snapshot).ConfigureAwait(false);
            var outputDirectory = "./simplicity-report";
            await ReportGenerator.GenerateHtmlReportAsync(
                snapshot,
                solutionPath,
                outputDirectory,
                Console.Error,
                configuration.Filters.ToFilterThresholds()).ConfigureAwait(false);
            Console.WriteLine($"Report generated to {Path.Combine(outputDirectory, "index.html")}");
            Console.WriteLine($"Snapshot saved to {SnapshotHistory.GetDirectoryPath(solutionPath)}");
            return 0;
        });
    }

    private static Task<int> RunBaselineAsync(string[] args)
    {
        return RunWithSnapshotAsync(args, static async (snapshot, _, solutionPath) =>
        {
            var baselinePath = await BaselineSnapshotFile.WriteAsync(solutionPath, snapshot).ConfigureAwait(false);
            await SnapshotHistory.AppendAsync(solutionPath, snapshot).ConfigureAwait(false);

            Console.WriteLine(snapshot.ToSummary());
            Console.WriteLine();
            Console.WriteLine($"Baseline written to {baselinePath}");
            Console.WriteLine($"Snapshot saved to {SnapshotHistory.GetDirectoryPath(solutionPath)}");
            return 0;
        });
    }

    private static async Task<int> RunDiffAsync(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            WriteUsage();
            return 1;
        }

        var failOnRegression = false;
        if (args.Length == 2)
        {
            if (!string.Equals(args[1], "--fail-on-regression", StringComparison.OrdinalIgnoreCase))
            {
                WriteUsage();
                return 1;
            }

            failOnRegression = true;
        }

        return await RunWithSnapshotAsync([args[0]], async (currentSnapshot, configuration, solutionPath) =>
        {
            var baselinePath = BaselineSnapshotFile.GetPath(solutionPath);
            var baselineSnapshot = await BaselineSnapshotFile.ReadAsync(solutionPath).ConfigureAwait(false);
            var report = SnapshotDiffReportBuilder.Create(
                baselinePath,
                baselineSnapshot,
                currentSnapshot,
                configuration.Filters.ToFilterThresholds());

            Console.WriteLine(report.Content);
            return failOnRegression && report.HasRegression ? 1 : 0;
        }).ConfigureAwait(false);
    }

    private static Task<int> RunBudgetAsync(string[] args)
    {
        return RunWithSnapshotAsync(args, static (snapshot, configuration, _) =>
        {
            Console.WriteLine(ComplexityBudgetReportBuilder.Create(snapshot, configuration.Filters));
            return Task.FromResult(0);
        });
    }

    private static async Task<int> RunWithSnapshotAsync(
        string[] args,
        Func<SimplicitySnapshot, SimplicityConfiguration, string, Task<int>> action)
    {
        if (args.Length != 1)
        {
            WriteUsage();
            return 1;
        }

        var solutionPath = args[0];
        using var cancellationSource = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var configuration = SimplicityConfigurationLoader.LoadForSolution(solutionPath, Console.Error);
            var collector = new SimplicityCollector(Console.Error.WriteLine);
            var snapshot = await collector.CollectAsync(solutionPath, cancellationSource.Token).ConfigureAwait(false);
            return await action(snapshot, configuration, solutionPath).ConfigureAwait(false);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static async Task<int> RunWatchAsync(string[] args)
    {
        if (args.Length != 1)
        {
            WriteUsage();
            return 1;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;
        cancelHandler = static (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
        };

        cancelHandler += (_, _) => cancellationTokenSource.Cancel();
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var runner = new WatchCommandRunner(args[0], Console.Out, Console.Error);
            return await runner.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static void WriteUsage() => WriteUsage(Console.Out);

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet simplicity analyze <solution.sln>");
        writer.WriteLine("  dotnet simplicity report <solution.sln>");
        writer.WriteLine("  dotnet simplicity baseline <solution.sln>");
        writer.WriteLine("  dotnet simplicity diff <solution.sln> [--fail-on-regression]");
        writer.WriteLine("  dotnet simplicity budget <solution.sln>");
        writer.WriteLine("  dotnet simplicity watch <solution.sln>");
    }
}
