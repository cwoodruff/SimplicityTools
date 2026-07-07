using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal static class SnapshotFilterEvaluation
{
    private static readonly FilterName[] OrderedFilters =
    [
        FilterName.TwoAmTest,
        FilterName.HalfRule,
        FilterName.PrimaryPathFirst
    ];

    public static IReadOnlyList<FilterName> GetFilterOrder() => OrderedFilters;

    public static IReadOnlyDictionary<FilterName, FilterVerdict> Evaluate(SimplicitySnapshot snapshot) =>
        Evaluate(snapshot, FilterThresholds.Default);

    public static IReadOnlyDictionary<FilterName, FilterVerdict> Evaluate(SimplicitySnapshot snapshot, FilterThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(thresholds);

        return new Dictionary<FilterName, FilterVerdict>
        {
            [FilterName.TwoAmTest] = TwoAmTestEvaluator.Evaluate(snapshot, thresholds),
            [FilterName.HalfRule] = HalfRuleEvaluator.Evaluate(snapshot, thresholds),
            [FilterName.PrimaryPathFirst] = PrimaryPathFirstEvaluator.Evaluate(snapshot, thresholds)
        };
    }
}
