using System.Globalization;
using System.Text;
using SimplicityTools.Metrics;

internal static class ReportGenerator
{
    private const string BrandBackgroundColor = "#0D0D0D";
    private const string BrandAccentColor = "#E31B23";

    public static async Task GenerateHtmlReportAsync(SimplicitySnapshot snapshot, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var reportPath = Path.Combine(outputDirectory, "index.html");

        var html = GenerateHtmlContent(snapshot);
        await File.WriteAllTextAsync(reportPath, html, Encoding.UTF8).ConfigureAwait(false);
    }

    private static string GenerateHtmlContent(SimplicitySnapshot snapshot)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>Simplicity Report</title>");
        sb.AppendLine(GenerateEmbeddedStyles());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(GenerateHeader());
        sb.AppendLine(GenerateExecutiveSummary(snapshot));
        sb.AppendLine(GenerateFilterVerdicts(snapshot));
        sb.AppendLine(GenerateMetricDetail(snapshot));
        sb.AppendLine(GenerateComplexityBudget(snapshot));
        sb.AppendLine(GenerateTrendAnalysis(snapshot));
        sb.AppendLine(GenerateAppendix(snapshot));
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GenerateEmbeddedStyles()
    {
        return $$"""
          <style>
            * {
              margin: 0;
              padding: 0;
              box-sizing: border-box;
            }

            body {
              font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
              background-color: {{BrandBackgroundColor}};
              color: #f0f0f0;
              line-height: 1.6;
              padding: 0;
            }

            .container {
              max-width: 1200px;
              margin: 0 auto;
              padding: 40px 20px;
            }

            header {
              background-color: rgba(227, 27, 35, 0.1);
              border-bottom: 3px solid {{BrandAccentColor}};
              padding: 40px 0;
              margin-bottom: 40px;
            }

            h1 {
              font-size: 2.5em;
              color: {{BrandAccentColor}};
              margin-bottom: 10px;
            }

            .subtitle {
              font-size: 1.1em;
              color: #a0a0a0;
            }

            h2 {
              font-size: 2em;
              color: {{BrandAccentColor}};
              margin-top: 50px;
              margin-bottom: 25px;
              padding-bottom: 15px;
              border-bottom: 2px solid {{BrandAccentColor}};
            }

            h3 {
              font-size: 1.3em;
              color: {{BrandAccentColor}};
              margin-top: 25px;
              margin-bottom: 15px;
            }

            .metric-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
              gap: 20px;
              margin-bottom: 30px;
            }

            .metric-card {
              background-color: rgba(255, 255, 255, 0.05);
              border: 1px solid {{BrandAccentColor}};
              border-radius: 8px;
              padding: 20px;
              transition: all 0.3s ease;
            }

            .metric-card:hover {
              background-color: rgba(227, 27, 35, 0.15);
              border-color: {{BrandAccentColor}};
            }

            .metric-label {
              font-size: 0.9em;
              color: #a0a0a0;
              text-transform: uppercase;
              letter-spacing: 1px;
              margin-bottom: 10px;
            }

            .metric-value {
              font-size: 2.5em;
              color: {{BrandAccentColor}};
              font-weight: bold;
            }

            .metric-subvalue {
              font-size: 0.95em;
              color: #c0c0c0;
              margin-top: 5px;
            }

            .verdict {
              background-color: rgba(255, 255, 255, 0.05);
              border-left: 4px solid {{BrandAccentColor}};
              padding: 15px;
              margin-bottom: 15px;
              border-radius: 4px;
            }

            .verdict-title {
              font-weight: bold;
              color: {{BrandAccentColor}};
              margin-bottom: 5px;
            }

            .verdict-description {
              color: #d0d0d0;
              font-size: 0.95em;
            }

            .table {
              width: 100%;
              border-collapse: collapse;
              margin: 20px 0;
              background-color: rgba(255, 255, 255, 0.05);
            }

            .table thead {
              background-color: rgba(227, 27, 35, 0.2);
            }

            .table th {
              color: {{BrandAccentColor}};
              padding: 15px;
              text-align: left;
              font-weight: 600;
              border-bottom: 2px solid {{BrandAccentColor}};
            }

            .table td {
              padding: 12px 15px;
              border-bottom: 1px solid rgba(227, 27, 35, 0.1);
            }

            .table tr:hover {
              background-color: rgba(227, 27, 35, 0.1);
            }

            .highlight {
              color: {{BrandAccentColor}};
              font-weight: bold;
            }

            .section {
              margin-bottom: 40px;
            }

            .status-good {
              color: #2ecc71;
            }

            .status-warn {
              color: #f39c12;
            }

            .status-critical {
              color: {{BrandAccentColor}};
            }

            footer {
              margin-top: 60px;
              padding-top: 20px;
              border-top: 1px solid rgba(227, 27, 35, 0.2);
              color: #808080;
              font-size: 0.9em;
              text-align: center;
            }

            .badge {
              display: inline-block;
              padding: 5px 10px;
              border-radius: 4px;
              font-size: 0.85em;
              font-weight: 600;
              margin-right: 5px;
            }

            .badge-good {
              background-color: rgba(46, 204, 113, 0.2);
              color: #2ecc71;
            }

            .badge-warn {
              background-color: rgba(243, 156, 18, 0.2);
              color: #f39c12;
            }

            .badge-critical {
              background-color: rgba(227, 27, 35, 0.2);
              color: {{BrandAccentColor}};
            }
          </style>
        """;
    }

    private static string GenerateHeader()
    {
        return """
            <header>
              <div class="container">
                <h1>Simplicity Report</h1>
                <p class="subtitle">Complexity Analysis & Simplicity Metrics</p>
              </div>
            </header>
            """;
    }

    private static string GenerateExecutiveSummary(SimplicitySnapshot snapshot)
    {
        var collectedDate = snapshot.CollectedAt.ToString("MMMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture);
        var onboardingHours = snapshot.EstimatedOnboardingTime.TotalHours.ToString("F1", CultureInfo.InvariantCulture);

        return $"""
            <div class="container section">
              <h2>Executive Summary</h2>
              <p>This report provides a comprehensive analysis of your solution's complexity profile as of <span class="highlight">{collectedDate}</span>.</p>
              <div class="metric-grid">
                <div class="metric-card">
                  <div class="metric-label">Total Projects</div>
                  <div class="metric-value">{snapshot.TotalProjects}</div>
                </div>
                <div class="metric-card">
                  <div class="metric-label">Total Files</div>
                  <div class="metric-value">{snapshot.TotalFiles}</div>
                </div>
                <div class="metric-card">
                  <div class="metric-label">Primary Path</div>
                  <div class="metric-value">{snapshot.PrimaryPathFileCount}</div>
                  <div class="metric-subvalue">{(snapshot.PrimaryPathRatio * 100):F1}% of codebase</div>
                </div>
                <div class="metric-card">
                  <div class="metric-label">Estimated Onboarding</div>
                  <div class="metric-value">{onboardingHours}h</div>
                </div>
              </div>
            </div>
            """;
    }

    private static string GenerateFilterVerdicts(SimplicitySnapshot snapshot)
    {
        var prematureAbstractionScore = (snapshot.PrematureAbstractionRatio * 100).ToString("F0", CultureInfo.InvariantCulture);

        return $"""
            <div class="container section">
              <h2>Filter Verdicts</h2>
              <div class="verdict">
                <div class="verdict-title">Primary Path Coverage</div>
                <div class="verdict-description">
                  {snapshot.PrimaryPathFileCount} of {snapshot.TotalFiles} files ({(snapshot.PrimaryPathRatio * 100):F1}%) are on the primary path.
                  {(snapshot.PrimaryPathRatio > 0.8 ? "<span class=\"badge badge-good\">✓ Good</span>" : snapshot.PrimaryPathRatio > 0.5 ? "<span class=\"badge badge-warn\">⚠ Review</span>" : "<span class=\"badge badge-critical\">✗ Critical</span>")}
                </div>
              </div>
              <div class="verdict">
                <div class="verdict-title">Abstraction Health</div>
                <div class="verdict-description">
                  {snapshot.InterfacesWithSingleImplementation} of {snapshot.AbstractionLayerCount} interfaces have a single implementation ({prematureAbstractionScore}% premature abstraction).
                  {(snapshot.PrematureAbstractionRatio < 0.3 ? "<span class=\"badge badge-good\">✓ Good</span>" : snapshot.PrematureAbstractionRatio < 0.6 ? "<span class=\"badge badge-warn\">⚠ Review</span>" : "<span class=\"badge badge-critical\">✗ Critical</span>")}
                </div>
              </div>
              <div class="verdict">
                <div class="verdict-title">Dependency Management</div>
                <div class="verdict-description">
                  {snapshot.ExternalDependencyCount} external dependencies with {snapshot.UnusedDependencyCount} unused.
                  {(snapshot.UnusedDependencyCount == 0 ? "<span class=\"badge badge-good\">✓ Clean</span>" : snapshot.UnusedDependencyCount <= 2 ? "<span class=\"badge badge-warn\">⚠ Minor</span>" : "<span class=\"badge badge-critical\">✗ Needs Attention</span>")}
                </div>
              </div>
            </div>
            """;
    }

    private static string GenerateMetricDetail(SimplicitySnapshot snapshot)
    {
        var avgComplexity = snapshot.AverageMethodComplexity.ToString("F2", CultureInfo.InvariantCulture);
        var prematureAbstractRatio = (snapshot.PrematureAbstractionRatio * 100).ToString("F1", CultureInfo.InvariantCulture);

        return $"""
            <div class="container section">
              <h2>Metric Detail</h2>
              <table class="table">
                <thead>
                  <tr>
                    <th>Metric</th>
                    <th>Value</th>
                    <th>Interpretation</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>Total Projects</td>
                    <td class="highlight">{snapshot.TotalProjects}</td>
                    <td>Number of projects in the solution</td>
                  </tr>
                  <tr>
                    <td>Total Files</td>
                    <td class="highlight">{snapshot.TotalFiles}</td>
                    <td>Number of .cs files analyzed</td>
                  </tr>
                  <tr>
                    <td>Primary Path Files</td>
                    <td class="highlight">{snapshot.PrimaryPathFileCount}</td>
                    <td>Files on the main business logic path</td>
                  </tr>
                  <tr>
                    <td>Primary Path Ratio</td>
                    <td class="highlight">{(snapshot.PrimaryPathRatio * 100):F1}%</td>
                    <td>Percentage of codebase on primary path</td>
                  </tr>
                  <tr>
                    <td>Abstraction Layers</td>
                    <td class="highlight">{snapshot.AbstractionLayerCount}</td>
                    <td>Distinct interfaces declared</td>
                  </tr>
                  <tr>
                    <td>Single-Implementation Interfaces</td>
                    <td class="highlight">{snapshot.InterfacesWithSingleImplementation}</td>
                    <td>Interfaces with only one implementation</td>
                  </tr>
                  <tr>
                    <td>Premature Abstraction Ratio</td>
                    <td class="highlight">{prematureAbstractRatio}%</td>
                    <td>Percentage of abstraction that may be unnecessary</td>
                  </tr>
                  <tr>
                    <td>External Dependencies</td>
                    <td class="highlight">{snapshot.ExternalDependencyCount}</td>
                    <td>Number of NuGet packages in use</td>
                  </tr>
                  <tr>
                    <td>Unused Dependencies</td>
                    <td class="highlight">{snapshot.UnusedDependencyCount}</td>
                    <td>Dependencies not referenced in code</td>
                  </tr>
                  <tr>
                    <td>Average Method Complexity</td>
                    <td class="highlight">{avgComplexity}</td>
                    <td>Average cyclomatic complexity per method</td>
                  </tr>
                  <tr>
                    <td>Estimated Onboarding Time</td>
                    <td class="highlight">{snapshot.EstimatedOnboardingTime.TotalHours:F1} hours</td>
                    <td>Estimated time for a developer to understand the codebase</td>
                  </tr>
                </tbody>
              </table>
            </div>
            """;
    }

    private static string GenerateComplexityBudget(SimplicitySnapshot snapshot)
    {
        var onboardingHours = snapshot.EstimatedOnboardingTime.TotalHours;
        var complexity = snapshot.AverageMethodComplexity;

        var complexityStatus = complexity switch
        {
            < 3 => ("Excellent", "status-good"),
            < 5 => ("Good", "status-good"),
            < 10 => ("Moderate", "status-warn"),
            _ => ("High", "status-critical")
        };

        var onboardingStatus = onboardingHours switch
        {
            < 16 => ("Efficient", "status-good"),
            < 40 => ("Moderate", "status-warn"),
            _ => ("Substantial", "status-critical")
        };

        return $"""
            <div class="container section">
              <h2>Complexity Budget</h2>
              <p>Your solution's complexity profile impacts team velocity, onboarding time, and maintenance burden.</p>
              <div class="metric-grid">
                <div class="metric-card">
                  <div class="metric-label">Method Complexity</div>
                  <div class="metric-value {complexityStatus.Item2}">{complexity:F2}</div>
                  <div class="metric-subvalue">{complexityStatus.Item1}</div>
                </div>
                <div class="metric-card">
                  <div class="metric-label">Onboarding Time</div>
                  <div class="metric-value {onboardingStatus.Item2}">{onboardingHours:F1}h</div>
                  <div class="metric-subvalue">{onboardingStatus.Item1}</div>
                </div>
                <div class="metric-card">
                  <div class="metric-label">Simplicity Score</div>
                  <div class="metric-value">{CalculateSimplicityScore(snapshot):F0}/100</div>
                  <div class="metric-subvalue">Overall assessment</div>
                </div>
              </div>
            </div>
            """;
    }

    private static string GenerateTrendAnalysis(SimplicitySnapshot snapshot)
    {
        return """
            <div class="container section">
              <h2>Trend Analysis</h2>
              <p>This is the baseline snapshot for your solution. Future measurements will be compared against these values to identify trends in complexity growth, onboarding burden, and code health.</p>
              <div class="verdict">
                <div class="verdict-title">Baseline Established</div>
                <div class="verdict-description">
                  Your simplicity metrics have been recorded as a baseline. Monitor these metrics over time to track:
                  <ul style="margin-top: 10px; margin-left: 20px; color: #d0d0d0;">
                    <li>Primary path file count growth</li>
                    <li>Abstraction layer accumulation</li>
                    <li>Unused dependency trends</li>
                    <li>Average method complexity drift</li>
                    <li>Estimated onboarding time changes</li>
                  </ul>
                </div>
              </div>
            </div>
            """;
    }

    private static string GenerateAppendix(SimplicitySnapshot snapshot)
    {
        var collectedDate = snapshot.CollectedAt.ToString("O", CultureInfo.InvariantCulture);

        return $"""
            <div class="container section">
              <h2>Appendix</h2>
              <h3>Report Metadata</h3>
              <table class="table">
                <tbody>
                  <tr>
                    <td><strong>Report Generated</strong></td>
                    <td>{DateTime.UtcNow:O}</td>
                  </tr>
                  <tr>
                    <td><strong>Snapshot Collected</strong></td>
                    <td>{collectedDate}</td>
                  </tr>
                  <tr>
                    <td><strong>Simplicity Toolkit</strong></td>
                    <td>Version 1.0</td>
                  </tr>
                </tbody>
              </table>
              <h3>Metric Definitions</h3>
              <p><strong>Primary Path:</strong> The core business logic execution path through the codebase. High primary path coverage indicates focused, maintainable code organization.</p>
              <p><strong>Abstraction Layers:</strong> Distinct interfaces declared in the solution. Not all abstraction is necessary; unnecessary interfaces complicate the codebase.</p>
              <p><strong>Single-Implementation Interfaces:</strong> Interfaces with only one implementation. These are often premature abstraction and can be eliminated.</p>
              <p><strong>External Dependencies:</strong> NuGet packages referenced in the solution. Each dependency carries cognitive and maintenance cost.</p>
              <p><strong>Average Method Complexity:</strong> The cyclomatic complexity averaged across all methods. Higher values indicate more branching and control flow.</p>
              <p><strong>Estimated Onboarding Time:</strong> Approximate hours required for a new developer to understand the codebase. Based on file count, complexity, and abstraction.</p>
            </div>
            <footer>
              <p>This report was generated by the Simplicity-First .NET Toolkit. Learn more at <a href="https://github.com/cwoodruff/SimplicityTools" style="color: {BrandAccentColor};">https://github.com/cwoodruff/SimplicityTools</a></p>
            </footer>
            """;
    }

    private static double CalculateSimplicityScore(SimplicitySnapshot snapshot)
    {
        var score = 100.0;

        // Penalize for premature abstraction
        score -= snapshot.PrematureAbstractionRatio * 30;

        // Penalize for unused dependencies
        score -= Math.Min(snapshot.UnusedDependencyCount * 3, 20);

        // Penalize for high average complexity
        var complexityPenalty = Math.Max(0, (snapshot.AverageMethodComplexity - 3) / 10 * 20);
        score -= complexityPenalty;

        // Penalize for low primary path coverage
        if (snapshot.PrimaryPathRatio < 0.5)
            score -= (0.5 - snapshot.PrimaryPathRatio) * 30;

        return Math.Max(0, Math.Min(100, score));
    }
}
