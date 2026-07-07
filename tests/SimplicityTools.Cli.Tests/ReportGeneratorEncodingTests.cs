using System.Globalization;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Cli.Tests;

public sealed class ReportGeneratorEncodingTests
{
    [Theory]
    [InlineData("<script>alert('x')</script>", "&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;")]
    [InlineData("May 1, 2026 & later", "May 1, 2026 &amp; later")]
    [InlineData("\"quoted\" label", "&quot;quoted&quot; label")]
    public void Html_EncodesMarkupSignificantCharacters(string value, string expected)
    {
        Assert.Equal(expected, ReportGenerator.Html(value));
    }

    [Fact]
    public void Html_TreatsNullAsEmpty()
    {
        Assert.Equal(string.Empty, ReportGenerator.Html(null));
    }

    [Fact]
    public async Task GeneratedReport_ContainsNoUnencodedInjectionMarkers()
    {
        var workspace = Path.Combine(
            CliTestSupport.GetRepositoryPath("tests", "SimplicityTools.Cli.Tests", ".workspace"),
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(workspace);

        try
        {
            var solutionPath = Path.Combine(workspace, "Encoded.sln");
            await File.WriteAllTextAsync(solutionPath, "Microsoft Visual Studio Solution File, Format Version 12.00");

            var snapshot = new SimplicitySnapshot
            {
                TotalProjects = 1,
                TotalFiles = 10,
                PrimaryPathFileCount = 4,
                AbstractionLayerCount = 2,
                ExternalDependencyCount = 1,
                UnusedDependencyCount = 0,
                InterfacesWithSingleImplementation = 1,
                AverageMethodComplexity = 2.5d,
                EstimatedOnboardingTime = TimeSpan.FromHours(12),
                CollectedAt = new DateTimeOffset(2026, 07, 01, 9, 0, 0, TimeSpan.Zero)
            };

            var outputDirectory = Path.Combine(workspace, "simplicity-report");
            await ReportGenerator.GenerateHtmlReportAsync(snapshot, solutionPath, outputDirectory);

            var html = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "index.html"));

            // All dynamic text is routed through the Html() encoder, so a report generated from
            // any snapshot must never carry an unencoded script tag outside the known markup.
            Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("July 1, 2026", html);
        }
        finally
        {
            CliTestSupport.DeleteDirectoryIfExists(workspace);
        }
    }
}
