using SimplicityTools.Cli;
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

    private static async Task<int> RunAnalyzeAsync(string[] args)
    {
        if (args.Length != 1)
        {
            WriteUsage();
            return 1;
        }

        _ = SimplicityConfigurationLoader.LoadForSolution(args[0], Console.Error);
        var collector = new SimplicityCollector();
        var snapshot = await collector.CollectAsync(args[0]).ConfigureAwait(false);
        Console.WriteLine(snapshot.ToSummary());
        return 0;
    }

    private static async Task<int> RunReportAsync(string[] args)
    {
        if (args.Length != 1)
        {
            WriteUsage();
            return 1;
        }

        _ = SimplicityConfigurationLoader.LoadForSolution(args[0], Console.Error);
        var collector = new SimplicityCollector();
        var snapshot = await collector.CollectAsync(args[0]).ConfigureAwait(false);
        var outputDirectory = "./simplicity-report";
        await ReportGenerator.GenerateHtmlReportAsync(snapshot, args[0], outputDirectory).ConfigureAwait(false);
        Console.WriteLine($"Report generated to {Path.Combine(outputDirectory, "index.html")}");
        return 0;
    }

    private static async Task<int> RunBaselineAsync(string[] args)
    {
        if (args.Length != 1)
        {
            WriteUsage();
            return 1;
        }

        _ = SimplicityConfigurationLoader.LoadForSolution(args[0], Console.Error);
        var collector = new SimplicityCollector();
        var snapshot = await collector.CollectAsync(args[0]).ConfigureAwait(false);
        var baselinePath = await BaselineSnapshotFile.WriteAsync(args[0], snapshot).ConfigureAwait(false);

        Console.WriteLine(snapshot.ToSummary());
        Console.WriteLine();
        Console.WriteLine($"Baseline written to {baselinePath}");
        return 0;
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

        _ = SimplicityConfigurationLoader.LoadForSolution(args[0], Console.Error);
        var collector = new SimplicityCollector();
        var currentSnapshot = await collector.CollectAsync(args[0]).ConfigureAwait(false);
        var baselinePath = BaselineSnapshotFile.GetPath(args[0]);
        var baselineSnapshot = await BaselineSnapshotFile.ReadAsync(args[0]).ConfigureAwait(false);
        var report = SnapshotDiffReportBuilder.Create(baselinePath, baselineSnapshot, currentSnapshot);

        Console.WriteLine(report.Content);
        return failOnRegression && report.HasRegression ? 1 : 0;
    }

    private static async Task<int> RunBudgetAsync(string[] args)
    {
        if (args.Length != 1)
        {
            WriteUsage();
            return 1;
        }

        var configuration = SimplicityConfigurationLoader.LoadForSolution(args[0], Console.Error);
        var collector = new SimplicityCollector();
        var snapshot = await collector.CollectAsync(args[0]).ConfigureAwait(false);

        Console.WriteLine(ComplexityBudgetReportBuilder.Create(snapshot, configuration.Filters));
        return 0;
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
