using System.Reflection;

namespace SimplicityTools.Cli;

internal static class CliHelp
{
    public static string GetInformationalVersion()
    {
        var assembly = typeof(CliHelp).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "unknown";
    }

    public static void WriteRootUsage(TextWriter writer)
    {
        writer.WriteLine("Simplicity-First .NET Toolkit CLI");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet simplicity <command> <solution.sln> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands:");

        foreach (var command in CliCommands.All)
        {
            writer.WriteLine($"  {command.Name,-9} {command.Description}");
        }

        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  -h, --help   Show help for the CLI or, after a command, for that command.");
        writer.WriteLine("  --version    Print the tool version.");
        writer.WriteLine();
        WriteExitCodes(writer);
        writer.WriteLine();
        WriteFileLocations(writer);
    }

    public static void WriteCommandHelp(CliCommandDefinition command, TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine($"  dotnet simplicity {command.Name} <solution.sln> {FormatOptionSynopsis(command)}".TrimEnd());
        writer.WriteLine();
        writer.WriteLine(command.Description);
        writer.WriteLine();
        writer.WriteLine("Options:");

        foreach (var option in command.Options)
        {
            var invocation = option.TakesValue ? $"{option.Name} {option.ValuePlaceholder}" : option.Name;
            writer.WriteLine($"  {invocation,-22} {option.Description}");
        }

        writer.WriteLine($"  {"-h, --help",-22} Show this help.");
        writer.WriteLine();
        WriteExitCodes(writer);
        writer.WriteLine();
        WriteFileLocations(writer);
    }

    private static string FormatOptionSynopsis(CliCommandDefinition command)
    {
        var parts = command.Options
            .Select(option => option.TakesValue ? $"[{option.Name} {option.ValuePlaceholder}]" : $"[{option.Name}]");
        return string.Join(' ', parts);
    }

    private static void WriteExitCodes(TextWriter writer)
    {
        writer.WriteLine("Exit codes:");
        writer.WriteLine("  0  Success.");
        writer.WriteLine("  1  Error (bad arguments, analysis failure) or regression with --fail-on-regression.");
    }

    private static void WriteFileLocations(TextWriter writer)
    {
        writer.WriteLine("Files (all created/read next to the solution file):");
        writer.WriteLine("  simplicity.json            Optional thresholds/TCA configuration; defaults apply when absent.");
        writer.WriteLine("  .simplicity-baseline.json  Baseline snapshot written by 'baseline', read by 'diff'.");
        writer.WriteLine("  .simplicity-history/       Snapshot history appended by 'report' and 'baseline'.");
    }
}
