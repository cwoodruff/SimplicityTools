using SimplicityTools.Cli;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

return await CommandLineEntryPoint.RunAsync(args);

internal static class CommandLineEntryPoint
{
    public static async Task<int> RunAsync(string[] args)
    {
        var verbose = args.Any(static arg => string.Equals(arg, "--verbose", StringComparison.OrdinalIgnoreCase));

        try
        {
            return await DispatchAsync(args).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CliErrorReporter.Report(exception, verbose, Console.Error);
        }
    }

    private static async Task<int> DispatchAsync(string[] args)
    {
        if (args.Length == 0 || IsRootHelpToken(args[0]))
        {
            CliHelp.WriteRootUsage(Console.Out);
            return 0;
        }

        if (string.Equals(args[0], "--version", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(CliHelp.GetInformationalVersion());
            return 0;
        }

        if (!Commands.TryGetValue(args[0], out var command))
        {
            Console.Error.WriteLine($"Unknown command '{args[0]}'.");
            CliHelp.WriteRootUsage(Console.Error);
            return 1;
        }

        return await RunParsedCommandAsync(command.Definition, args[1..], command.Handler).ConfigureAwait(false);
    }

    private static readonly Dictionary<string, (CliCommandDefinition Definition, Func<CommandArguments, Task<int>> Handler)> Commands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [CliCommands.Analyze.Name] = (CliCommands.Analyze, RunAnalyzeAsync),
            [CliCommands.Report.Name] = (CliCommands.Report, RunReportAsync),
            [CliCommands.Baseline.Name] = (CliCommands.Baseline, RunBaselineAsync),
            [CliCommands.Diff.Name] = (CliCommands.Diff, RunDiffAsync),
            [CliCommands.Budget.Name] = (CliCommands.Budget, RunBudgetAsync),
            [CliCommands.Watch.Name] = (CliCommands.Watch, RunWatchAsync)
        };

    private static bool IsRootHelpToken(string arg)
    {
        return CommandArgumentParser.IsHelpToken(arg) ||
               string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> RunParsedCommandAsync(
        CliCommandDefinition definition,
        string[] args,
        Func<CommandArguments, Task<int>> handler)
    {
        var parseResult = CommandArgumentParser.Parse(definition, args);

        if (parseResult.ShowHelp)
        {
            CliHelp.WriteCommandHelp(definition, Console.Out);
            return 0;
        }

        if (parseResult.Error is not null)
        {
            Console.Error.WriteLine(parseResult.Error);
            CliHelp.WriteCommandHelp(definition, Console.Error);
            return 1;
        }

        return await handler(parseResult.Arguments!).ConfigureAwait(false);
    }

    private static Task<int> RunAnalyzeAsync(CommandArguments arguments)
    {
        return RunWithSnapshotAsync(arguments, (snapshot, _, _) =>
        {
            Console.WriteLine(arguments.WantsJson ? SnapshotEnvelope.Serialize(snapshot) : snapshot.ToSummary());
            return Task.FromResult(0);
        });
    }

    private static Task<int> RunReportAsync(CommandArguments arguments)
    {
        return RunWithSnapshotAsync(arguments, async (snapshot, configuration, solutionPath) =>
        {
            await SnapshotHistory.AppendAsync(solutionPath, snapshot).ConfigureAwait(false);
            var outputDirectory = ResolveReportDirectory(arguments, solutionPath);
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

    private static string ResolveReportDirectory(CommandArguments arguments, string solutionPath)
    {
        if (arguments.GetValue("--output") is { } outputDirectory)
        {
            return Path.GetFullPath(outputDirectory);
        }

        var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionPath))
            ?? throw new InvalidOperationException($"Could not determine the directory for '{solutionPath}'.");
        return Path.Combine(solutionDirectory, "simplicity-report");
    }

    private static Task<int> RunBaselineAsync(CommandArguments arguments)
    {
        return RunWithSnapshotAsync(arguments, static async (snapshot, _, solutionPath) =>
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

    private static Task<int> RunDiffAsync(CommandArguments arguments)
    {
        var failOnRegression = arguments.HasFlag("--fail-on-regression");

        return RunWithSnapshotAsync(arguments, async (currentSnapshot, configuration, solutionPath) =>
        {
            var baselinePath = BaselineSnapshotFile.GetPath(solutionPath);
            var baselineSnapshot = await BaselineSnapshotFile.ReadAsync(solutionPath).ConfigureAwait(false);
            var thresholds = configuration.Filters.ToFilterThresholds();
            var result = SnapshotDiffReportBuilder.CreateResult(baselineSnapshot, currentSnapshot, thresholds);

            Console.WriteLine(arguments.WantsJson
                ? CliJsonOutput.SerializeDiff(result)
                : SnapshotDiffReportBuilder.Render(baselinePath, result, thresholds).Content);

            return failOnRegression && result.HasRegression ? 1 : 0;
        });
    }

    private static Task<int> RunBudgetAsync(CommandArguments arguments)
    {
        return RunWithSnapshotAsync(arguments, (snapshot, configuration, _) =>
        {
            var result = ComplexityBudgetReportBuilder.CreateResult(snapshot, configuration.Filters);
            Console.WriteLine(arguments.WantsJson
                ? CliJsonOutput.SerializeBudget(result)
                : ComplexityBudgetReportBuilder.Render(result));
            return Task.FromResult(0);
        });
    }

    private static async Task<int> RunWithSnapshotAsync(
        CommandArguments arguments,
        Func<SimplicitySnapshot, SimplicityConfiguration, string, Task<int>> action)
    {
        var solutionPath = arguments.SolutionPath;
        EnsureSolutionFileExists(solutionPath);

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

    private static void EnsureSolutionFileExists(string solutionPath)
    {
        if (!File.Exists(solutionPath))
        {
            var fullPath = Path.GetFullPath(solutionPath);
            throw new FileNotFoundException($"Solution file was not found at '{fullPath}'.", fullPath);
        }
    }

    private static async Task<int> RunWatchAsync(CommandArguments arguments)
    {
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
            var runner = new WatchCommandRunner(arguments.SolutionPath, Console.Out, Console.Error);
            return await runner.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
