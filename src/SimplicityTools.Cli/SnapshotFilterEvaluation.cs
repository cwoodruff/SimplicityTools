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

    public static IReadOnlyDictionary<FilterName, FilterVerdict> Evaluate(SimplicitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new Dictionary<FilterName, FilterVerdict>
        {
            [FilterName.TwoAmTest] = TwoAmTestEvaluator.Evaluate(snapshot),
            [FilterName.HalfRule] = HalfRuleEvaluator.Evaluate(snapshot),
            [FilterName.PrimaryPathFirst] = PrimaryPathFirstEvaluator.Evaluate(snapshot)
        };
    }
}
