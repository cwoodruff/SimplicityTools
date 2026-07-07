using System.Text.Json;
using System.Text.Json.Serialization;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

/// <summary>
/// Machine-readable JSON payloads for the diff and budget commands. Snapshots use the same
/// camelCase shape as the persisted <see cref="SnapshotEnvelope" /> files.
/// </summary>
internal static class CliJsonOutput
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // UsageFraction is positive infinity when a minimum-threshold metric measures zero.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static string SerializeDiff(SnapshotDiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var payload = new DiffPayload(result.Baseline, result.Current, result.Regressions, result.HasRegression);
        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public static string SerializeBudget(ComplexityBudgetResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var dimensions = result.Dimensions
            .Select(static dimension => new BudgetDimensionPayload(
                dimension.Name,
                dimension.MetricLabel,
                dimension.Actual,
                dimension.Target,
                dimension.UsageFraction,
                dimension.IsWithinBudget))
            .ToArray();

        var payload = new BudgetPayload(
            dimensions,
            result.WithinBudgetCount,
            result.Dimensions.Count - result.WithinBudgetCount,
            result.OnboardingTimeComputed ? [] : ["Cognitive Load"]);

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private sealed record DiffPayload(
        SimplicitySnapshot Baseline,
        SimplicitySnapshot Current,
        IReadOnlyList<string> Regressions,
        bool HasRegression);

    private sealed record BudgetDimensionPayload(
        string Name,
        string MetricLabel,
        double Actual,
        double Target,
        double UsageFraction,
        bool WithinBudget);

    private sealed record BudgetPayload(
        IReadOnlyList<BudgetDimensionPayload> Dimensions,
        int WithinBudgetCount,
        int OverBudgetCount,
        IReadOnlyList<string> NotScored);
}
