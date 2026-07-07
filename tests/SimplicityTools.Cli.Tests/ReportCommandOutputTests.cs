using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class ReportCommandOutputTests
{
    [Fact]
    public async Task ReportCommand_DefaultsToSolutionDirectoryAndPrintsFullPath()
    {
        var workspace = CliTestSupport.CreateSampleWorkspace("Sample.Simplified");

        try
        {
            var solutionPath = Path.Combine(workspace, "Sample.Simplified.sln");
            var result = await CliTestSupport.RunCliAsync("report", solutionPath);

            Assert.Equal(0, result.ExitCode);
            var expectedReportPath = Path.Combine(workspace, "simplicity-report", "index.html");
            Assert.True(File.Exists(expectedReportPath), $"Report file not found at {expectedReportPath}");
            Assert.Contains($"Report generated to {expectedReportPath}", result.StandardOutput);
        }
        finally
        {
            CliTestSupport.DeleteDirectoryIfExists(workspace);
        }
    }

    [Fact]
    public async Task ReportCommand_OutputOptionOverridesDirectoryAndPrintsFullPath()
    {
        var workspace = CliTestSupport.CreateSampleWorkspace("Sample.Simplified");
        var customDirectory = Path.Combine(workspace, "artifacts", "custom-report");

        try
        {
            var solutionPath = Path.Combine(workspace, "Sample.Simplified.sln");
            var result = await CliTestSupport.RunCliAsync("report", "--output", customDirectory, solutionPath);

            Assert.Equal(0, result.ExitCode);
            var expectedReportPath = Path.Combine(customDirectory, "index.html");
            Assert.True(File.Exists(expectedReportPath), $"Report file not found at {expectedReportPath}");
            Assert.Contains($"Report generated to {expectedReportPath}", result.StandardOutput);
            Assert.False(
                Directory.Exists(Path.Combine(workspace, "simplicity-report")),
                "--output should fully replace the default report directory.");
        }
        finally
        {
            CliTestSupport.DeleteDirectoryIfExists(workspace);
        }
    }
}
