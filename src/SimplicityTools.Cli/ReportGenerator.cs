using System.Globalization;
using System.Text;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal static class ReportGenerator
{
    private const string BrandBackgroundColor = "#0D0D0D";
    private const string BrandAccentColor = "#E31B23";
    private const string PrimaryPathColor = "#4DD0E1";
    private const string ComplexityColor = "#FFB74D";
    private const string OnboardingColor = "#AB47BC";

    public static async Task GenerateHtmlReportAsync(SimplicitySnapshot snapshot, string solutionPath, string outputDirectory, TextWriter? diagnosticsWriter = null, FilterThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(solutionPath);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        var reportPath = Path.Combine(outputDirectory, "index.html");
        var historicalSnapshots = await SnapshotHistory.ReadAsync(solutionPath, diagnosticsWriter ?? TextWriter.Null).ConfigureAwait(false);

        var html = GenerateHtmlContent(snapshot, historicalSnapshots, thresholds ?? FilterThresholds.Default);
        await File.WriteAllTextAsync(reportPath, html, Encoding.UTF8).ConfigureAwait(false);
    }

    private static string GenerateHtmlContent(SimplicitySnapshot snapshot, IReadOnlyList<SimplicitySnapshot> historicalSnapshots, FilterThresholds thresholds)
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
        sb.AppendLine(GenerateFilterVerdicts(snapshot, thresholds));
        sb.AppendLine(GenerateMetricDetail(snapshot));
        sb.AppendLine(GenerateComplexityBudget(snapshot, thresholds));
        sb.AppendLine(GenerateTrendAnalysis(snapshot, historicalSnapshots, thresholds));
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

            .metric-card,
            .trend-card {
              background-color: rgba(255, 255, 255, 0.05);
              border: 1px solid {{BrandAccentColor}};
              border-radius: 8px;
              padding: 20px;
              transition: all 0.3s ease;
            }

            .metric-card:hover,
            .trend-card:hover {
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
              vertical-align: top;
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

            .delta-better {
              color: #2ecc71;
              font-weight: 600;
            }

            .delta-worse {
              color: {{BrandAccentColor}};
              font-weight: 600;
            }

            .delta-flat {
              color: #c0c0c0;
            }

            .trend-stack {
              display: grid;
              gap: 20px;
            }

            .trend-card {
              overflow-x: auto;
            }

            .trend-card h3 {
              margin-top: 0;
            }

            .trend-note {
              color: #c8c8c8;
              margin-bottom: 18px;
            }

            .trend-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 12px 18px;
              margin-bottom: 18px;
              font-size: 0.9em;
              color: #d0d0d0;
            }

            .trend-legend-item {
              display: inline-flex;
              align-items: center;
              gap: 8px;
            }

            .trend-swatch {
              width: 12px;
              height: 12px;
              border-radius: 999px;
              display: inline-block;
            }

            .trend-chart {
              width: 100%;
              height: auto;
              display: block;
            }

            .chart-axis-label,
            .chart-value-label,
            .chart-point-label,
            .chart-series-label {
              font-family: inherit;
            }

            .current-run {
              white-space: nowrap;
            }

            code {
              background-color: rgba(255, 255, 255, 0.08);
              border-radius: 4px;
              padding: 2px 6px;
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
                <p class="subtitle">Complexity Analysis &amp; Simplicity Metrics</p>
              </div>
            </header>
            """;
    }

    private static string GenerateExecutiveSummary(SimplicitySnapshot snapshot)
    {
        var collectedDate = snapshot.CollectedAt.ToString("MMMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture);
        var onboardingDisplay = snapshot.EstimatedOnboardingTime is { } measuredOnboarding
            ? measuredOnboarding.TotalHours.ToString("F1", CultureInfo.InvariantCulture) + "h"
            : "Not yet measured";

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
                  <div class="metric-value">{onboardingDisplay}</div>
                </div>
              </div>
            </div>
            """;
    }

    private static string GenerateFilterVerdicts(SimplicitySnapshot snapshot, FilterThresholds thresholds)
    {
        var prematureAbstractionScore = (snapshot.PrematureAbstractionRatio * 100).ToString("F0", CultureInfo.InvariantCulture);

        return $"""
            <div class="container section">
              <h2>Filter Verdicts</h2>
              <div class="verdict">
                <div class="verdict-title">Primary Path Coverage</div>
                <div class="verdict-description">
                  {snapshot.PrimaryPathFileCount} of {snapshot.TotalFiles} files ({(snapshot.PrimaryPathRatio * 100):F1}%) are on the primary path.
                  {FormatBadge(SimplicityScoring.GetPrimaryPathBand(snapshot.PrimaryPathRatio, thresholds))}
                </div>
              </div>
              <div class="verdict">
                <div class="verdict-title">Abstraction Health</div>
                <div class="verdict-description">
                  {snapshot.InterfacesWithSingleImplementation} of {snapshot.AbstractionLayerCount} interfaces have a single implementation ({prematureAbstractionScore}% premature abstraction).
                  {FormatBadge(SimplicityScoring.GetPrematureAbstractionBand(snapshot.PrematureAbstractionRatio, thresholds))}
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
                    <td class="highlight">{(snapshot.EstimatedOnboardingTime is { } tableOnboarding ? $"{tableOnboarding.TotalHours:F1} hours" : "Not yet measured")}</td>
                    <td>Estimated time for a developer to understand the codebase</td>
                  </tr>
                </tbody>
              </table>
            </div>
            """;
    }

    private static string GenerateComplexityBudget(SimplicitySnapshot snapshot, FilterThresholds thresholds)
    {
        var complexity = snapshot.AverageMethodComplexity;
        var complexityBand = SimplicityScoring.GetComplexityBand(complexity, thresholds);
        var complexityStatus = (complexityBand.Label, ToCssClass(complexityBand.Severity));

        // No verdict is rendered for a metric that was never computed.
        var (onboardingValue, onboardingVerdict, onboardingClass) = snapshot.EstimatedOnboardingTime is { } onboardingTime
            ? FormatOnboardingBand(onboardingTime, thresholds)
            : ("Not yet measured", "Metric not computed", string.Empty);

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
                  <div class="metric-value {onboardingClass}">{onboardingValue}</div>
                  <div class="metric-subvalue">{onboardingVerdict}</div>
                </div>
                <div class="metric-card">
                  <div class="metric-label">Simplicity Score</div>
                  <div class="metric-value">{SimplicityScoring.CalculateScore(snapshot, thresholds):F0}/100</div>
                  <div class="metric-subvalue">Overall assessment</div>
                </div>
              </div>
            </div>
            """;
    }

    private static string GenerateTrendAnalysis(SimplicitySnapshot snapshot, IReadOnlyList<SimplicitySnapshot> historicalSnapshots, FilterThresholds thresholds)
    {
        if (historicalSnapshots.Count < 2)
        {
            return GenerateTrendOnRamp(historicalSnapshots.Count);
        }

        var trendPoints = BuildTrendPoints(snapshot, historicalSnapshots, thresholds);
        var historicalCount = historicalSnapshots.Count;

        return $"""
            <div class="container section">
              <h2>Trend Analysis</h2>
              <p>This report found <span class="highlight">{historicalCount}</span> historical snapshots in <code>.simplicity-history/</code> and layered the current report on top so teams can see where complexity is drifting.</p>
              <div class="trend-stack">
                <div class="trend-card">
                  <h3>Trend Wave</h3>
                  <p class="trend-note">The SVG chart tracks primary path coverage, average method complexity{(trendPoints.All(trendPoint => trendPoint.Snapshot.EstimatedOnboardingTime is not null) ? ", onboarding hours," : " and")} simplicity score over time with exact values rendered below.</p>
                  {GenerateTrendLegend(trendPoints.All(trendPoint => trendPoint.Snapshot.EstimatedOnboardingTime is not null))}
                  {GenerateTrendChart(trendPoints)}
                </div>
                <div class="trend-card">
                  <h3>Historical Filter Scores</h3>
                  <p class="trend-note">Filter scores are shown as percentages so teams can see whether the solution is becoming easier or harder to work with.</p>
                  {GenerateHistoricalFilterScoreTable(trendPoints)}
                </div>
                <div class="trend-card">
                  <h3>Complexity Deltas</h3>
                  <p class="trend-note">Deltas compare each snapshot with the one immediately before it. Positive numbers mean more complexity cost unless noted otherwise.</p>
                  {GenerateComplexityDeltaTable(trendPoints)}
                </div>
              </div>
            </div>
            """;
    }

    private static string GenerateTrendOnRamp(int historicalCount)
    {
        var noun = historicalCount == 1 ? "snapshot" : "snapshots";

        return $"""
            <div class="container section">
              <h2>Trend Analysis</h2>
              <p>Trend analysis unlocks when <code>.simplicity-history/</code> contains at least two snapshots. This report found <span class="highlight">{historicalCount}</span> historical {noun}, so the current run is acting as your teaching baseline.</p>
              <div class="verdict">
                <div class="verdict-title">What to do next</div>
                <div class="verdict-description">
                  Each <code>report</code> or <code>baseline</code> run saves a snapshot into <code>.simplicity-history/</code> automatically. Run the report again after your next change and this section will render an inline SVG trend chart, historical filter scores, and complexity deltas.
                </div>
              </div>
            </div>
            """;
    }

    private static IReadOnlyList<TrendPoint> BuildTrendPoints(SimplicitySnapshot currentSnapshot, IReadOnlyList<SimplicitySnapshot> historicalSnapshots, FilterThresholds thresholds)
    {
        // The current run is identified by instance, not by timestamp-tick equality: a history
        // entry written for this run is replaced by the in-memory snapshot so exactly one point
        // is marked current.
        var snapshots = historicalSnapshots
            .Where(snapshot => !ReferenceEquals(snapshot, currentSnapshot) && snapshot != currentSnapshot)
            .Select(snapshot => (Snapshot: snapshot, IsCurrent: false))
            .Append((Snapshot: currentSnapshot, IsCurrent: true));

        return snapshots
            .OrderBy(static entry => entry.Snapshot.CollectedAt)
            .Select(entry => new TrendPoint(
                FormatChartDate(entry.Snapshot.CollectedAt),
                entry.IsCurrent,
                entry.Snapshot,
                SnapshotFilterEvaluation.Evaluate(entry.Snapshot, thresholds),
                SimplicityScoring.CalculateScore(entry.Snapshot, thresholds)))
            .ToArray();
    }

    private static string ToCssClass(ScoreBandSeverity severity) => severity switch
    {
        ScoreBandSeverity.Good => "status-good",
        ScoreBandSeverity.Warn => "status-warn",
        _ => "status-critical"
    };

    private static string FormatBadge(ScoreBand band)
    {
        var (symbol, cssClass) = band.Severity switch
        {
            ScoreBandSeverity.Good => ("✓", "badge-good"),
            ScoreBandSeverity.Warn => ("⚠", "badge-warn"),
            _ => ("✗", "badge-critical")
        };

        return $"<span class=\"badge {cssClass}\">{symbol} {band.Label}</span>";
    }

    private static (string Value, string Verdict, string CssClass) FormatOnboardingBand(TimeSpan onboardingTime, FilterThresholds thresholds)
    {
        var band = SimplicityScoring.GetOnboardingBand(onboardingTime.TotalHours, thresholds);
        return ($"{onboardingTime.TotalHours:F1}h", band.Label, ToCssClass(band.Severity));
    }

    private static string GenerateTrendLegend(bool includeOnboarding)
    {
        var onboardingItem = includeOnboarding
            ? $"""<span class="trend-legend-item"><span class="trend-swatch" style="background: {OnboardingColor};"></span>Onboarding Hours</span>"""
            : string.Empty;

        return $$"""
            <div class="trend-legend">
              <span class="trend-legend-item"><span class="trend-swatch" style="background: {{PrimaryPathColor}};"></span>Primary Path Coverage</span>
              <span class="trend-legend-item"><span class="trend-swatch" style="background: {{ComplexityColor}};"></span>Avg Method Complexity</span>
              {{onboardingItem}}
              <span class="trend-legend-item"><span class="trend-swatch" style="background: {{BrandAccentColor}};"></span>Simplicity Score</span>
            </div>
            """;
    }

    private static string GenerateTrendChart(IReadOnlyList<TrendPoint> trendPoints)
    {
        var metrics = new List<TrendMetric>
        {
            new("Primary Path Coverage", trendPoint => trendPoint.Snapshot.PrimaryPathRatio * 100, PrimaryPathColor, "F1", "%", 0d, 100d),
            new("Avg Method Complexity", trendPoint => trendPoint.Snapshot.AverageMethodComplexity, ComplexityColor, "F2", string.Empty, 0d, null)
        };

        // Charting an uncomputed metric would draw a fabricated flat line.
        if (trendPoints.All(trendPoint => trendPoint.Snapshot.EstimatedOnboardingTime is not null))
        {
            metrics.Add(new TrendMetric("Onboarding Hours", trendPoint => trendPoint.Snapshot.EstimatedOnboardingTime!.Value.TotalHours, OnboardingColor, "F1", "h", 0d, null));
        }

        metrics.Add(new TrendMetric("Simplicity Score", trendPoint => trendPoint.SimplicityScore, BrandAccentColor, "F0", string.Empty, 0d, 100d));

        const double width = 1080d;
        const double height = 620d;
        const double left = 84d;
        const double right = 28d;
        const double top = 28d;
        const double panelHeight = 105d;
        const double panelGap = 38d;
        var chartWidth = width - left - right;
        var fullChartHeight = metrics.Count * panelHeight + (metrics.Count - 1) * panelGap;

        var sb = new StringBuilder();
        sb.AppendLine($"<svg class=\"trend-chart\" viewBox=\"0 0 {width.ToString(CultureInfo.InvariantCulture)} {height.ToString(CultureInfo.InvariantCulture)}\" role=\"img\" aria-label=\"Historical trend chart for primary path coverage, average method complexity, onboarding hours, and simplicity score.\">");
        sb.AppendLine($"<rect x=\"0\" y=\"0\" width=\"{width.ToString(CultureInfo.InvariantCulture)}\" height=\"{height.ToString(CultureInfo.InvariantCulture)}\" rx=\"16\" fill=\"rgba(255,255,255,0.03)\" />");

        if (trendPoints[^1].IsCurrent)
        {
            var currentX = GetXPosition(trendPoints.Count, trendPoints.Count - 1, left, chartWidth);
            sb.AppendLine($"<line x1=\"{FormatNumber(currentX)}\" y1=\"{FormatNumber(top)}\" x2=\"{FormatNumber(currentX)}\" y2=\"{FormatNumber(top + fullChartHeight)}\" stroke=\"rgba(227, 27, 35, 0.28)\" stroke-dasharray=\"6 6\" />");
            sb.AppendLine($"<text class=\"chart-series-label\" x=\"{FormatNumber(currentX - 48d)}\" y=\"20\" fill=\"{BrandAccentColor}\" font-size=\"11\">Current report</text>");
        }

        for (var index = 0; index < metrics.Count; index++)
        {
            AppendTrendMetricPanel(
                sb,
                metrics[index],
                trendPoints,
                left,
                top + (index * (panelHeight + panelGap)),
                chartWidth,
                panelHeight,
                showXAxisLabels: index == metrics.Count - 1);
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void AppendTrendMetricPanel(
        StringBuilder builder,
        TrendMetric metric,
        IReadOnlyList<TrendPoint> trendPoints,
        double left,
        double top,
        double width,
        double height,
        bool showXAxisLabels)
    {
        var values = trendPoints.Select(metric.Selector).ToArray();
        var (min, max) = GetTrendAxisRange(metric, values);
        var bottom = top + height;

        builder.AppendLine($"<text class=\"chart-series-label\" x=\"16\" y=\"{FormatNumber(top + 14d)}\" fill=\"{metric.Color}\" font-size=\"12\" font-weight=\"600\">{metric.Title}</text>");
        builder.AppendLine($"<rect x=\"{FormatNumber(left)}\" y=\"{FormatNumber(top)}\" width=\"{FormatNumber(width)}\" height=\"{FormatNumber(height)}\" fill=\"rgba(255,255,255,0.02)\" rx=\"8\" />");

        for (var tickIndex = 0; tickIndex <= 4; tickIndex++)
        {
            var ratio = tickIndex / 4d;
            var y = bottom - (ratio * height);
            var tickValue = min + ((max - min) * ratio);
            builder.AppendLine($"<line x1=\"{FormatNumber(left)}\" y1=\"{FormatNumber(y)}\" x2=\"{FormatNumber(left + width)}\" y2=\"{FormatNumber(y)}\" stroke=\"rgba(255,255,255,0.10)\" stroke-width=\"1\" />");
            builder.AppendLine($"<text class=\"chart-axis-label\" x=\"12\" y=\"{FormatNumber(y + 4d)}\" fill=\"#9a9a9a\" font-size=\"11\">{FormatValue(tickValue, metric.Format, metric.UnitSuffix)}</text>");
        }

        var points = new List<string>(trendPoints.Count);
        for (var index = 0; index < trendPoints.Count; index++)
        {
            var x = GetXPosition(trendPoints.Count, index, left, width);
            var y = bottom - (((values[index] - min) / (max - min)) * height);
            points.Add($"{FormatNumber(x)},{FormatNumber(y)}");
        }

        builder.AppendLine($"<polyline points=\"{string.Join(' ', points)}\" fill=\"none\" stroke=\"{metric.Color}\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />");

        AppendTrendDataPoints(builder, metric, trendPoints, values, min, max, left, width, height, bottom);

        var lastValue = values[^1];
        var lastX = GetXPosition(trendPoints.Count, trendPoints.Count - 1, left, width);
        var lastY = bottom - (((lastValue - min) / (max - min)) * height);
        builder.AppendLine($"<text class=\"chart-value-label\" x=\"{FormatNumber(lastX + 10d)}\" y=\"{FormatNumber(lastY - 8d)}\" fill=\"{metric.Color}\" font-size=\"11\">{FormatValue(lastValue, metric.Format, metric.UnitSuffix)}</text>");

        if (showXAxisLabels)
        {
            AppendTrendXAxisLabels(builder, trendPoints, left, width, bottom);
        }
    }

    private static (double Min, double Max) GetTrendAxisRange(TrendMetric metric, IReadOnlyList<double> values)
    {
        var min = metric.Minimum ?? values.Min();
        var max = metric.Maximum ?? values.Max();

        if (max <= min)
        {
            max = min + 1d;
        }

        var padding = metric.Maximum is null ? Math.Max((max - min) * 0.1d, 0.5d) : 0d;
        min = metric.Minimum ?? Math.Max(0d, min - padding);
        max = metric.Maximum ?? max + padding;
        return (min, max);
    }

    private static void AppendTrendDataPoints(
        StringBuilder builder,
        TrendMetric metric,
        IReadOnlyList<TrendPoint> trendPoints,
        IReadOnlyList<double> values,
        double min,
        double max,
        double left,
        double width,
        double height,
        double bottom)
    {
        for (var index = 0; index < trendPoints.Count; index++)
        {
            var x = GetXPosition(trendPoints.Count, index, left, width);
            var y = bottom - (((values[index] - min) / (max - min)) * height);
            var radius = trendPoints[index].IsCurrent ? 5d : 4d;
            var fill = trendPoints[index].IsCurrent ? BrandAccentColor : metric.Color;
            builder.AppendLine($"<circle cx=\"{FormatNumber(x)}\" cy=\"{FormatNumber(y)}\" r=\"{FormatNumber(radius)}\" fill=\"{fill}\" stroke=\"#f0f0f0\" stroke-width=\"1.5\" />");
        }
    }

    private static void AppendTrendXAxisLabels(
        StringBuilder builder,
        IReadOnlyList<TrendPoint> trendPoints,
        double left,
        double width,
        double bottom)
    {
        for (var index = 0; index < trendPoints.Count; index++)
        {
            var x = GetXPosition(trendPoints.Count, index, left, width);
            builder.AppendLine($"<text class=\"chart-point-label\" x=\"{FormatNumber(x)}\" y=\"{FormatNumber(bottom + 18d)}\" fill=\"#b6b6b6\" font-size=\"11\" text-anchor=\"middle\">{trendPoints[index].Label}</text>");
        }
    }

    private static string GenerateHistoricalFilterScoreTable(IReadOnlyList<TrendPoint> trendPoints)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<table class=\"table\">");
        builder.AppendLine("  <thead>");
        builder.AppendLine("    <tr>");
        builder.AppendLine("      <th>Snapshot</th>");
        builder.AppendLine("      <th>TwoAmTest</th>");
        builder.AppendLine("      <th>HalfRule</th>");
        builder.AppendLine("      <th>PrimaryPathFirst</th>");
        builder.AppendLine("      <th>Simplicity Score</th>");
        builder.AppendLine("    </tr>");
        builder.AppendLine("  </thead>");
        builder.AppendLine("  <tbody>");

        foreach (var trendPoint in trendPoints)
        {
            builder.AppendLine("    <tr>");
            builder.AppendLine($"      <td>{FormatTableDate(trendPoint.Snapshot.CollectedAt)}{(trendPoint.IsCurrent ? " <span class=\"badge badge-critical current-run\">Current report</span>" : string.Empty)}</td>");

            foreach (var filter in SnapshotFilterEvaluation.GetFilterOrder())
            {
                var verdict = trendPoint.Verdicts[filter];
                var scoreClass = verdict.Passes ? "status-good" : "status-warn";
                var badgeClass = verdict.Passes ? "badge-good" : "badge-warn";
                var badgeText = verdict.Passes ? "Pass" : "Review";
                builder.AppendLine($"      <td><span class=\"{scoreClass}\">{verdict.Score * 100:F0}%</span><div class=\"metric-subvalue\"><span class=\"badge {badgeClass}\">{badgeText}</span></div></td>");
            }

            builder.AppendLine($"      <td class=\"highlight\">{trendPoint.SimplicityScore:F0}/100</td>");
            builder.AppendLine("    </tr>");
        }

        builder.AppendLine("  </tbody>");
        builder.AppendLine("</table>");
        return builder.ToString();
    }

    private static string GenerateComplexityDeltaTable(IReadOnlyList<TrendPoint> trendPoints)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<table class=\"table\">");
        builder.AppendLine("  <thead>");
        builder.AppendLine("    <tr>");
        builder.AppendLine("      <th>Snapshot</th>");
        builder.AppendLine("      <th>Avg Complexity</th>");
        builder.AppendLine("      <th>Δ</th>");
        builder.AppendLine("      <th>Onboarding</th>");
        builder.AppendLine("      <th>Δ</th>");
        builder.AppendLine("      <th>Abstraction Layers</th>");
        builder.AppendLine("      <th>Δ</th>");
        builder.AppendLine("      <th>Unused Deps</th>");
        builder.AppendLine("      <th>Δ</th>");
        builder.AppendLine("    </tr>");
        builder.AppendLine("  </thead>");
        builder.AppendLine("  <tbody>");

        TrendPoint? previous = null;
        foreach (var trendPoint in trendPoints)
        {
            builder.AppendLine("    <tr>");
            builder.AppendLine($"      <td>{FormatTableDate(trendPoint.Snapshot.CollectedAt)}{(trendPoint.IsCurrent ? " <span class=\"badge badge-critical current-run\">Current report</span>" : string.Empty)}</td>");
            builder.AppendLine($"      <td class=\"highlight\">{trendPoint.Snapshot.AverageMethodComplexity:F2}</td>");
            builder.AppendLine($"      <td>{FormatWorseningDelta(previous is null ? null : trendPoint.Snapshot.AverageMethodComplexity - previous.Snapshot.AverageMethodComplexity, "F2")}</td>");
            builder.AppendLine($"      <td class=\"highlight\">{(trendPoint.Snapshot.EstimatedOnboardingTime is { } rowOnboarding ? $"{rowOnboarding.TotalHours:F1}h" : "—")}</td>");
            builder.AppendLine($"      <td>{FormatWorseningDelta(trendPoint.Snapshot.EstimatedOnboardingTime is { } currentOnboarding && previous?.Snapshot.EstimatedOnboardingTime is { } previousOnboarding ? currentOnboarding.TotalHours - previousOnboarding.TotalHours : null, "F1", "h")}</td>");
            builder.AppendLine($"      <td class=\"highlight\">{trendPoint.Snapshot.AbstractionLayerCount}</td>");
            builder.AppendLine($"      <td>{FormatWorseningDelta(previous is null ? null : trendPoint.Snapshot.AbstractionLayerCount - previous.Snapshot.AbstractionLayerCount)}</td>");
            builder.AppendLine($"      <td class=\"highlight\">{trendPoint.Snapshot.UnusedDependencyCount}</td>");
            builder.AppendLine($"      <td>{FormatWorseningDelta(previous is null ? null : trendPoint.Snapshot.UnusedDependencyCount - previous.Snapshot.UnusedDependencyCount)}</td>");
            builder.AppendLine("    </tr>");

            previous = trendPoint;
        }

        builder.AppendLine("  </tbody>");
        builder.AppendLine("</table>");
        return builder.ToString();
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
              <p>This report was generated by the Simplicity-First .NET Toolkit.</p>
            </footer>
            """;
    }


    private static string FormatWorseningDelta(int? delta)
    {
        if (delta is null)
        {
            return "<span class=\"delta-flat\">Baseline</span>";
        }

        return FormatDelta(delta.Value, delta.Value > 0, delta.Value < 0, string.Empty);
    }

    private static string FormatWorseningDelta(double? delta, string format, string suffix = "")
    {
        if (delta is null)
        {
            return "<span class=\"delta-flat\">Baseline</span>";
        }

        return FormatDelta(delta.Value.ToString(format, CultureInfo.InvariantCulture), delta.Value > 0, delta.Value < 0, suffix);
    }

    private static string FormatDelta(int value, bool isWorse, bool isBetter, string suffix)
    {
        var formatted = value > 0
            ? $"+{value.ToString(CultureInfo.InvariantCulture)}{suffix}"
            : $"{value.ToString(CultureInfo.InvariantCulture)}{suffix}";
        var cssClass = isWorse ? "delta-worse" : isBetter ? "delta-better" : "delta-flat";
        return $"<span class=\"{cssClass}\">{formatted}</span>";
    }

    private static string FormatDelta(string formattedValue, bool isWorse, bool isBetter, string suffix)
    {
        var displayValue = formattedValue.StartsWith("-", StringComparison.Ordinal)
            ? $"{formattedValue}{suffix}"
            : $"+{formattedValue}{suffix}";

        if (!isWorse && !isBetter)
        {
            displayValue = $"0{suffix}";
        }

        var cssClass = isWorse ? "delta-worse" : isBetter ? "delta-better" : "delta-flat";
        return $"<span class=\"{cssClass}\">{displayValue}</span>";
    }

    private static double GetXPosition(int totalPoints, int index, double left, double width)
    {
        if (totalPoints <= 1)
        {
            return left + (width / 2d);
        }

        return left + ((width * index) / (totalPoints - 1d));
    }

    private static string FormatChartDate(DateTimeOffset value) =>
        value.ToString("MMM d", CultureInfo.InvariantCulture);

    private static string FormatTableDate(DateTimeOffset value) =>
        value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);

    private static string FormatValue(double value, string format, string suffix) =>
        $"{value.ToString(format, CultureInfo.InvariantCulture)}{suffix}";

    private static string FormatNumber(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private sealed record TrendPoint(
        string Label,
        bool IsCurrent,
        SimplicitySnapshot Snapshot,
        IReadOnlyDictionary<FilterName, FilterVerdict> Verdicts,
        double SimplicityScore);

    private sealed record TrendMetric(
        string Title,
        Func<TrendPoint, double> Selector,
        string Color,
        string Format,
        string UnitSuffix,
        double? Minimum,
        double? Maximum);
}
