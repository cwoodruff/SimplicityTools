namespace SimplicityTools.Cli;

/// <summary>
/// Declares an option a command accepts. Options with a <paramref name="ValuePlaceholder" />
/// consume the next argv token as their value; the rest are boolean flags.
/// </summary>
internal sealed record CliOptionDefinition(
    string Name,
    string Description,
    string? ValuePlaceholder = null,
    IReadOnlyList<string>? AllowedValues = null)
{
    public bool TakesValue => ValuePlaceholder is not null;
}

internal sealed record CliCommandDefinition(
    string Name,
    string Description,
    IReadOnlyList<CliOptionDefinition> Options);

/// <summary>
/// Successfully parsed arguments for a command: the solution path positional plus any options.
/// </summary>
internal sealed record CommandArguments(string SolutionPath, IReadOnlyDictionary<string, string?> Options)
{
    public bool HasFlag(string name) => Options.ContainsKey(name);

    public string? GetValue(string name) => Options.TryGetValue(name, out var value) ? value : null;

    public bool WantsJson => string.Equals(GetValue("--format"), "json", StringComparison.OrdinalIgnoreCase);
}

internal sealed record CommandParseResult(bool ShowHelp, string? Error, CommandArguments? Arguments)
{
    public static CommandParseResult ForHelp() => new(ShowHelp: true, Error: null, Arguments: null);

    public static CommandParseResult ForError(string error) => new(ShowHelp: false, error, Arguments: null);

    public static CommandParseResult ForArguments(CommandArguments arguments) => new(ShowHelp: false, Error: null, arguments);
}

/// <summary>
/// Hand-rolled argument parser: one required solution-path positional plus the command's declared
/// options, accepted in any order. Kept in-tree instead of taking a System.CommandLine dependency
/// so the CLI stays dependency-light while central package management lands separately.
/// </summary>
internal static class CommandArgumentParser
{
    public static CommandParseResult Parse(CliCommandDefinition command, string[] args)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(args);

        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? solutionPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (IsHelpToken(token))
            {
                return CommandParseResult.ForHelp();
            }

            if (token.StartsWith('-'))
            {
                var error = ReadOption(command, args, ref index, options);
                if (error is not null)
                {
                    return CommandParseResult.ForError(error);
                }

                continue;
            }

            if (solutionPath is not null)
            {
                return CommandParseResult.ForError($"Unexpected argument '{token}'. The '{command.Name}' command takes a single <solution.sln> argument.");
            }

            solutionPath = token;
        }

        return solutionPath is null
            ? CommandParseResult.ForError($"Missing required <solution.sln> argument for the '{command.Name}' command.")
            : CommandParseResult.ForArguments(new CommandArguments(solutionPath, options));
    }

    private static string? ReadOption(
        CliCommandDefinition command,
        string[] args,
        ref int index,
        Dictionary<string, string?> options)
    {
        var token = args[index];
        var option = command.Options.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, token, StringComparison.OrdinalIgnoreCase));

        if (option is null)
        {
            return $"Unknown option '{token}' for the '{command.Name}' command.";
        }

        if (!option.TakesValue)
        {
            options[option.Name] = null;
            return null;
        }

        if (index + 1 >= args.Length)
        {
            return $"Option '{option.Name}' requires a value {option.ValuePlaceholder}.";
        }

        var value = args[++index];
        if (option.AllowedValues is { } allowed && !allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return $"Invalid value '{value}' for '{option.Name}'. Allowed values: {string.Join(", ", allowed)}.";
        }

        options[option.Name] = value;
        return null;
    }

    internal static bool IsHelpToken(string arg)
    {
        return string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(arg, "-h", StringComparison.Ordinal);
    }
}

/// <summary>
/// The command surface of the CLI. Descriptions double as the per-command help text.
/// </summary>
internal static class CliCommands
{
    private static readonly CliOptionDefinition FormatOption = new(
        "--format",
        "Output format: 'text' (default) or 'json'. JSON goes to stdout with nothing else, for CI consumption.",
        "<text|json>",
        ["text", "json"]);

    private static readonly CliOptionDefinition VerboseOption = new(
        "--verbose",
        "Print full exception details when the command fails.");

    private static readonly CliOptionDefinition FailOnRegressionOption = new(
        "--fail-on-regression",
        "Exit with code 1 when the diff detects one or more regressions.");

    private static readonly CliOptionDefinition OutputOption = new(
        "--output",
        "Directory to write the HTML report to. Defaults to <solution-directory>/simplicity-report.",
        "<directory>");

    public static readonly CliCommandDefinition Analyze = new(
        "analyze",
        "Analyze a solution and print a snapshot summary of its simplicity metrics.",
        [FormatOption, VerboseOption]);

    public static readonly CliCommandDefinition Report = new(
        "report",
        "Generate a self-contained HTML report and append a snapshot to .simplicity-history/.",
        [OutputOption, VerboseOption]);

    public static readonly CliCommandDefinition Baseline = new(
        "baseline",
        "Capture the current snapshot as .simplicity-baseline.json for future diff runs.",
        [VerboseOption]);

    public static readonly CliCommandDefinition Diff = new(
        "diff",
        "Compare the current snapshot against the saved baseline and list regressions.",
        [FormatOption, FailOnRegressionOption, VerboseOption]);

    public static readonly CliCommandDefinition Budget = new(
        "budget",
        "Show complexity budget usage across the configured budget dimensions.",
        [FormatOption, VerboseOption]);

    public static readonly CliCommandDefinition Watch = new(
        "watch",
        "Watch the solution and re-analyze on file changes, printing filter verdicts.",
        [VerboseOption]);

    public static readonly IReadOnlyList<CliCommandDefinition> All =
        [Analyze, Report, Baseline, Diff, Budget, Watch];
}
