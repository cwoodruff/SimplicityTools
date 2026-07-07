using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class CliErrorHandlingTests
{
    [Fact]
    public async Task MissingSolutionFile_PrintsActionableOneLinerWithoutStackTrace()
    {
        var missingSolution = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.sln");

        var result = await CliTestSupport.RunCliAsync("analyze", missingSolution);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"Error: solution file was not found at '{missingSolution}'.", result.StandardError);
        Assert.Contains("Re-run with --verbose", result.StandardError);
        Assert.DoesNotContain("FileNotFoundException", result.StandardError);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), result.StandardOutput);
    }

    [Fact]
    public async Task VerboseFlag_PrintsFullExceptionDetails()
    {
        var missingSolution = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.sln");

        var result = await CliTestSupport.RunCliAsync("analyze", "--verbose", missingSolution);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Error: solution file was not found", result.StandardError);
        Assert.Contains("FileNotFoundException", result.StandardError);
    }

    [Fact]
    public void ErrorReporter_TranslatesInvalidProjectFileExceptionByTypeName()
    {
        // The reporter matches by type name because Microsoft.Build types cannot be referenced
        // before MSBuildLocator registration; this stand-in exercises the same match.
        using var writer = new StringWriter();
        var exception = new InvalidProjectFileException("The solution file is malformed.");

        var exitCode = CliErrorReporter.Report(exception, verbose: false, writer);

        Assert.Equal(1, exitCode);
        Assert.Contains("Error: the solution or one of its projects could not be parsed: The solution file is malformed.", writer.ToString());
    }

    [Fact]
    public void ErrorReporter_TranslatesMsBuildNotFoundFailures()
    {
        using var writer = new StringWriter();
        var exception = new InvalidOperationException("No instances of MSBuild could be detected.");

        CliErrorReporter.Report(exception, verbose: false, writer);

        Assert.Contains("Error: no MSBuild/.NET SDK installation could be located.", writer.ToString());
    }

    [Fact]
    public void ErrorReporter_LeavesUnknownFailuresAsTheirOwnMessage()
    {
        using var writer = new StringWriter();
        var exception = new InvalidOperationException("Invalid simplicity.json: $.filters must be a JSON object.");

        CliErrorReporter.Report(exception, verbose: false, writer);

        Assert.Contains("Invalid simplicity.json", writer.ToString());
        Assert.DoesNotContain("Error: no MSBuild", writer.ToString());
    }

    private sealed class InvalidProjectFileException(string message) : Exception(message);
}
