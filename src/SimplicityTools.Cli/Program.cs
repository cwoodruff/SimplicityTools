using SimplicityTools.Metrics;

return await CommandLineEntryPoint.RunAsync(args);

internal static class CommandLineEntryPoint
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                WriteUsage();
                return 0;
            }

            if (string.Equals(args[0], "analyze", StringComparison.OrdinalIgnoreCase))
            {
                return await RunAnalyzeAsync(args[1..]).ConfigureAwait(false);
            }

            if (string.Equals(args[0], "report", StringComparison.OrdinalIgnoreCase))
            {
                return await RunReportAsync(args[1..]).ConfigureAwait(false);
            }

            WriteUsage();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> RunAnalyzeAsync(string[] args)
    {
        if (args.Length != 1)
        {
            WriteUsage();
            return 1;
        }

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

        var collector = new SimplicityCollector();
        var snapshot = await collector.CollectAsync(args[0]).ConfigureAwait(false);
        var outputDirectory = "./simplicity-report";
        await ReportGenerator.GenerateHtmlReportAsync(snapshot, outputDirectory).ConfigureAwait(false);
        Console.WriteLine($"Report generated to {Path.Combine(outputDirectory, "index.html")}");
        return 0;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet simplicity analyze <solution.sln>");
        Console.WriteLine("  dotnet simplicity report <solution.sln>");
    }
}
