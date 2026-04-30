using System.Globalization;
using System.Text;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal sealed record SnapshotDiffReport(string Content, bool HasRegression);

internal static class SnapshotDiffReportBuilder
{
    private const double PrematureAbstractionRegressionThreshold = 0.05d;
    private const double AverageMethodComplexityRegressionThreshold = 0.5d;
    private const double FilterScoreRegressionThreshold = 0.1d;

    public static SnapshotDiffReport Create(string baselinePath, SimplicitySnapshot baseline, SimplicitySnapshot current)
    {
        ArgumentNullException.ThrowIfNull(baselinePath);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        var regressions = new List<string>();
        var baselineVerdicts = EvaluateFilters(baseline);
        var currentVerdicts = EvaluateFilters(current);

        AddMetricRegression(
            regressions,
            "PrematureAbstractionRatio",
            current.PrematureAbstractionRatio - baseline.PrematureAbstractionRatio,
            PrematureAbstractionRegressionThreshold);

        AddMetricRegression(
            regressions,
            "AverageMethodComplexity",
            current.AverageMethodComplexity - baseline.AverageMethodComplexity,
            AverageMethodComplexityRegressionThreshold);

        if (current.UnusedDependencyCount > baseline.UnusedDependencyCount &&
            current.UnusedDependencyCount > 0)
        {
            regressions.Add(
                $"UnusedDependencyCount increased from {baseline.UnusedDependencyCount} to {current.UnusedDependencyCount}.");
        }

        foreach (var filter in GetFilterOrder())
        {
            var scoreDelta = currentVerdicts[filter].Score - baselineVerdicts[filter].Score;
            if (scoreDelta < -FilterScoreRegressionThreshold)
            {
                regressions.Add(
                    $"{filter} score decreased by {Math.Abs(scoreDelta).ToString("F2", CultureInfo.InvariantCulture)} (threshold -0.10).");
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine("Simplicity Diff");
        builder.AppendLine("---------------");
        builder.AppendLine($"Baseline file: {baselinePath}");
        builder.AppendLine($"Baseline snapshot: {baseline.CollectedAt:yyyy-MM-dd}");
        builder.AppendLine($"Current snapshot: {current.CollectedAt:yyyy-MM-dd}");
        builder.AppendLine();
        builder.AppendLine("Metric delta");
        builder.AppendLine($"- Total projects: {FormatIntMetric(baseline.TotalProjects, current.TotalProjects)}");
        builder.AppendLine($"- Total files: {FormatIntMetric(baseline.TotalFiles, current.TotalFiles)}");
        builder.AppendLine($"- Primary path files: {FormatIntMetric(baseline.PrimaryPathFileCount, current.PrimaryPathFileCount)}");
        builder.AppendLine($"- Abstraction layers: {FormatIntMetric(baseline.AbstractionLayerCount, current.AbstractionLayerCount)}");
        builder.AppendLine($"- Single-implementation interfaces: {FormatIntMetric(baseline.InterfacesWithSingleImplementation, current.InterfacesWithSingleImplementation)}");
        builder.AppendLine($"- Premature abstraction ratio: {FormatDoubleMetric(baseline.PrematureAbstractionRatio, current.PrematureAbstractionRatio)}");
        builder.AppendLine($"- External dependencies: {FormatIntMetric(baseline.ExternalDependencyCount, current.ExternalDependencyCount)}");
        builder.AppendLine($"- Unused dependencies: {FormatIntMetric(baseline.UnusedDependencyCount, current.UnusedDependencyCount)}");
        builder.AppendLine($"- Average method complexity: {FormatDoubleMetric(baseline.AverageMethodComplexity, current.AverageMethodComplexity)}");
        builder.AppendLine($"- Estimated onboarding time: {FormatHoursMetric(baseline.EstimatedOnboardingTime, current.EstimatedOnboardingTime)}");
        builder.AppendLine();
        builder.AppendLine("Filter score delta");

        foreach (var filter in GetFilterOrder())
        {
            builder.AppendLine(
                $"- {filter}: {FormatDoubleMetric(baselineVerdicts[filter].Score, currentVerdicts[filter].Score)}");
        }

        builder.AppendLine();
        builder.AppendLine(
            regressions.Count == 0
                ? "Regression status: no regressions detected."
                : $"Regression status: {regressions.Count} regression(s) detected.");

        foreach (var regression in regressions)
        {
            builder.AppendLine($"- {regression}");
        }

        return new SnapshotDiffReport(builder.ToString().TrimEnd(), regressions.Count > 0);
    }

    private static void AddMetricRegression(ICollection<string> regressions, string metricName, double delta, double threshold)
    {
        if (delta > threshold)
        {
            regressions.Add(
                $"{metricName} increased by {delta.ToString("F2", CultureInfo.InvariantCulture)} (threshold +{threshold.ToString("F2", CultureInfo.InvariantCulture)}).");
        }
    }

    private static IReadOnlyDictionary<FilterName, FilterVerdict> EvaluateFilters(SimplicitySnapshot snapshot)
    {
        return new Dictionary<FilterName, FilterVerdict>
        {
            [FilterName.TwoAmTest] = TwoAmTestEvaluator.Evaluate(snapshot),
            [FilterName.HalfRule] = HalfRuleEvaluator.Evaluate(snapshot),
            [FilterName.PrimaryPathFirst] = PrimaryPathFirstEvaluator.Evaluate(snapshot)
        };
    }

    private static IEnumerable<FilterName> GetFilterOrder()
    {
        yield return FilterName.TwoAmTest;
        yield return FilterName.HalfRule;
        yield return FilterName.PrimaryPathFirst;
    }

    private static string FormatIntMetric(int baseline, int current)
    {
        var delta = current - baseline;
        return $"{baseline} -> {current} ({FormatSignedInt(delta)})";
    }

    private static string FormatDoubleMetric(double baseline, double current)
    {
        var delta = current - baseline;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{baseline:F2} -> {current:F2} ({FormatSignedDouble(delta, "F2")})");
    }

    private static string FormatHoursMetric(TimeSpan baseline, TimeSpan current)
    {
        var delta = current.TotalHours - baseline.TotalHours;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{baseline.TotalHours:F1}h -> {current.TotalHours:F1}h ({FormatSignedDouble(delta, "F1")}h)");
    }

    private static string FormatSignedInt(int value)
    {
        return value > 0
            ? string.Create(CultureInfo.InvariantCulture, $"+{value}")
            : value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatSignedDouble(double value, string format)
    {
        var formatted = value.ToString(format, CultureInfo.InvariantCulture);
        return value > 0
            ? $"+{formatted}"
            : formatted;
    }
}
