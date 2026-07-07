using System.Globalization;
using System.Text;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal static class ComplexityBudgetReportBuilder
{
    public static string Create(SimplicitySnapshot snapshot, FilterThresholdConfiguration thresholds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(thresholds);

        var dimensions = new List<ComplexityBudgetDimension>();

        // Fabricating a "WITHIN BUDGET" verdict from an uncomputed metric would be a lie; the
        // dimension is reported as not computed instead of scored.
        if (snapshot.EstimatedOnboardingTime is { } onboardingTime)
        {
            dimensions.Add(CreateMaximumThresholdDimension(
                "Cognitive Load",
                "Onboarding time",
                onboardingTime.TotalHours,
                thresholds.MaxOnboardingHours,
                value => $"{value.ToString("F1", CultureInfo.InvariantCulture)}h",
                "Reduce spread and indirection so new engineers can learn the codebase faster."));
        }

        dimensions.AddRange(
        [
            CreateMaximumThresholdDimension(
                "Operational Surface",
                "Premature abstraction ratio",
                snapshot.PrematureAbstractionRatio,
                thresholds.PrematureAbstractionRatioTarget,
                value => value.ToString("F2", CultureInfo.InvariantCulture),
                "Remove single-use abstractions so the solution exposes fewer moving parts."),
            CreateMaximumThresholdDimension(
                "Change Safety",
                "Average method complexity",
                snapshot.AverageMethodComplexity,
                thresholds.MaxMethodComplexity,
                value => value.ToString("F2", CultureInfo.InvariantCulture),
                "Break up complex methods until routine changes stay easier to reason about."),
            CreateMinimumThresholdDimension(
                "Discoverability",
                "Primary path ratio",
                snapshot.PrimaryPathRatio,
                thresholds.PrimaryPathRatioTarget,
                value => value.ToString("F2", CultureInfo.InvariantCulture),
                "Move more of the main business flow onto the primary path so teams can trace it faster.")
        ]);

        var withinBudgetCount = dimensions.Count(static dimension => dimension.IsWithinBudget);
        var builder = new StringBuilder();

        builder.AppendLine("Complexity Budget");
        builder.AppendLine("-----------------");
        builder.AppendLine($"Status: {withinBudgetCount}/{dimensions.Count} dimension(s) within budget.");
        builder.AppendLine("Bars show configured budget used. Values above 100% are over budget.");
        builder.AppendLine();

        if (snapshot.EstimatedOnboardingTime is null)
        {
            builder.AppendLine("Cognitive Load      not scored — onboarding time has not been computed.");
        }

        foreach (var dimension in dimensions)
        {
            builder.AppendLine(
                $"{dimension.Name,-19} {CreateBar(dimension.UsageFraction)} {FormatUsage(dimension.UsageFraction),6}  {(dimension.IsWithinBudget ? "WITHIN BUDGET" : "OVER BUDGET")}");
            builder.AppendLine($"  {dimension.MetricLabel}: {dimension.ActualDisplay} (target {dimension.TargetDisplay})");
        }

        builder.AppendLine();

        var mostConstrained = dimensions
            .OrderByDescending(static dimension => dimension.UsageFraction)
            .First();

        builder.Append(
            mostConstrained.IsWithinBudget
                ? $"Next move: keep {mostConstrained.Name} under watch; it is closest to the configured limit."
                : $"Next move: {mostConstrained.Name} is {FormatOverage(mostConstrained.UsageFraction)} over budget. {mostConstrained.Guidance}");

        return builder.ToString().TrimEnd();
    }

    private static ComplexityBudgetDimension CreateMaximumThresholdDimension(
        string name,
        string metricLabel,
        double actual,
        double threshold,
        Func<double, string> formatter,
        string guidance)
    {
        var usageFraction = threshold == 0d ? 0d : actual / threshold;
        return new ComplexityBudgetDimension(
            name,
            metricLabel,
            formatter(actual),
            $"<= {formatter(threshold)}",
            usageFraction,
            actual <= threshold,
            guidance);
    }

    private static ComplexityBudgetDimension CreateMinimumThresholdDimension(
        string name,
        string metricLabel,
        double actual,
        double threshold,
        Func<double, string> formatter,
        string guidance)
    {
        var usageFraction = threshold <= 0d
            ? 0d
            : actual <= 0d
                ? double.PositiveInfinity
                : threshold / actual;

        return new ComplexityBudgetDimension(
            name,
            metricLabel,
            formatter(actual),
            $">= {formatter(threshold)}",
            usageFraction,
            actual >= threshold,
            guidance);
    }

    private static string CreateBar(double usageFraction)
    {
        var normalizedFraction = double.IsInfinity(usageFraction)
            ? 1d
            : Math.Clamp(usageFraction, 0d, 1d);
        var filledSegments = (int)Math.Round(normalizedFraction * 10d, MidpointRounding.AwayFromZero);
        filledSegments = Math.Clamp(filledSegments, 0, 10);

        return $"[{new string('#', filledSegments)}{new string('-', 10 - filledSegments)}]";
    }

    private static string FormatUsage(double usageFraction)
    {
        if (double.IsInfinity(usageFraction))
        {
            return "∞";
        }

        return $"{(usageFraction * 100d).ToString("F0", CultureInfo.InvariantCulture)}%";
    }

    private static string FormatOverage(double usageFraction)
    {
        if (double.IsInfinity(usageFraction))
        {
            return "well beyond the configured threshold";
        }

        return $"{((usageFraction - 1d) * 100d).ToString("F0", CultureInfo.InvariantCulture)}%";
    }

    private sealed record ComplexityBudgetDimension(
        string Name,
        string MetricLabel,
        string ActualDisplay,
        string TargetDisplay,
        double UsageFraction,
        bool IsWithinBudget,
        string Guidance);
}
