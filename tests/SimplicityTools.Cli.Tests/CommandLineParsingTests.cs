using System.Text.RegularExpressions;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class CommandLineParsingTests
{
    [Fact]
    public void Parse_AcceptsOptionsAnywhereInArgv()
    {
        var result = CommandArgumentParser.Parse(CliCommands.Diff, ["--format", "json", "solution.sln", "--fail-on-regression"]);

        Assert.Null(result.Error);
        Assert.False(result.ShowHelp);
        Assert.Equal("solution.sln", result.Arguments!.SolutionPath);
        Assert.True(result.Arguments.WantsJson);
        Assert.True(result.Arguments.HasFlag("--fail-on-regression"));
    }

    [Fact]
    public void Parse_ReportsUnknownOption()
    {
        var result = CommandArgumentParser.Parse(CliCommands.Analyze, ["solution.sln", "--fail-on-regression"]);

        Assert.Contains("Unknown option '--fail-on-regression'", result.Error);
        Assert.Null(result.Arguments);
    }

    [Fact]
    public void Parse_ReportsMissingSolutionArgument()
    {
        var result = CommandArgumentParser.Parse(CliCommands.Analyze, ["--format", "json"]);

        Assert.Contains("Missing required <solution.sln> argument", result.Error);
    }

    [Fact]
    public void Parse_ReportsUnexpectedSecondPositional()
    {
        var result = CommandArgumentParser.Parse(CliCommands.Analyze, ["one.sln", "two.sln"]);

        Assert.Contains("Unexpected argument 'two.sln'", result.Error);
    }

    [Fact]
    public void Parse_ReportsMissingOptionValue()
    {
        var result = CommandArgumentParser.Parse(CliCommands.Analyze, ["solution.sln", "--format"]);

        Assert.Contains("requires a value", result.Error);
    }

    [Fact]
    public void Parse_RejectsInvalidFormatValue()
    {
        var result = CommandArgumentParser.Parse(CliCommands.Analyze, ["solution.sln", "--format", "xml"]);

        Assert.Contains("Invalid value 'xml' for '--format'", result.Error);
        Assert.Contains("text, json", result.Error);
    }

    [Fact]
    public void Parse_TreatsHelpTokenAsHelpRequest()
    {
        Assert.True(CommandArgumentParser.Parse(CliCommands.Budget, ["--help"]).ShowHelp);
        Assert.True(CommandArgumentParser.Parse(CliCommands.Budget, ["solution.sln", "-h"]).ShowHelp);
    }

    [Fact]
    public async Task VersionFlag_PrintsInformationalVersionAndReturnsZero()
    {
        var result = await CliTestSupport.RunCliAsync("--version");

        Assert.Equal(0, result.ExitCode);
        // Assert the format: <semver>-local+<hex-commit-sha>. The exact SHA changes with every
        // commit, so comparing against the in-process assembly version is fragile (the test
        // assembly's copy of the CLI DLL can be stale relative to the freshly-built subprocess).
        // Asserting the format is both sufficient and commit-stable.
        Assert.Matches(@"^0\.5\.0-local\+[0-9a-f]+$", result.StandardOutput.Trim());
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError), result.StandardError);
    }

    [Fact]
    public async Task RootHelp_DocumentsCommandsExitCodesAndFileLocations()
    {
        var result = await CliTestSupport.RunCliAsync("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage:", result.StandardOutput);
        Assert.Contains("Exit codes:", result.StandardOutput);
        Assert.Contains(".simplicity-baseline.json", result.StandardOutput);
        Assert.Contains(".simplicity-history/", result.StandardOutput);
        Assert.Contains("simplicity.json", result.StandardOutput);

        foreach (var command in CliCommands.All)
        {
            Assert.Contains(command.Name, result.StandardOutput);
        }
    }

    [Fact]
    public async Task CommandHelp_ListsCommandOptions()
    {
        var result = await CliTestSupport.RunCliAsync("diff", "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("dotnet simplicity diff <solution.sln>", result.StandardOutput);
        Assert.Contains("--fail-on-regression", result.StandardOutput);
        Assert.Contains("--format", result.StandardOutput);
        Assert.Contains("Exit codes:", result.StandardOutput);
    }

    [Fact]
    public async Task UnknownOption_WritesCommandUsageToStandardErrorAndReturnsOne()
    {
        var result = await CliTestSupport.RunCliAsync("analyze", "--nope", "solution.sln");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown option '--nope'", result.StandardError);
        Assert.Contains("Usage:", result.StandardError);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), result.StandardOutput);
    }
}
