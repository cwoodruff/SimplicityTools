namespace SimplicityTools.Cli;

/// <summary>
/// Turns the known failure shapes into one-line actionable messages. The full exception is only
/// printed when the user opts in with --verbose.
/// </summary>
internal static class CliErrorReporter
{
    public static int Report(Exception exception, bool verbose, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine(GetActionableMessage(exception));

        if (verbose)
        {
            writer.WriteLine(exception.ToString());
        }
        else
        {
            writer.WriteLine("Re-run with --verbose for full exception details.");
        }

        return 1;
    }

    private static string GetActionableMessage(Exception exception)
    {
        if (exception is FileNotFoundException { FileName: { } fileName } &&
            fileName.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            return $"Error: solution file was not found at '{fileName}'. Check the path and pass an existing .sln file.";
        }

        // Matched by name: Microsoft.Build is loaded via MSBuildLocator, so its exception types
        // cannot be referenced before registration without risking assembly-load failures.
        if (string.Equals(exception.GetType().Name, "InvalidProjectFileException", StringComparison.Ordinal))
        {
            return $"Error: the solution or one of its projects could not be parsed: {exception.Message}";
        }

        if (exception is InvalidOperationException && exception.Message.Contains("MSBuild", StringComparison.OrdinalIgnoreCase))
        {
            return "Error: no MSBuild/.NET SDK installation could be located. Install the .NET SDK (https://dotnet.microsoft.com/download) and re-run.";
        }

        return exception.Message;
    }
}
