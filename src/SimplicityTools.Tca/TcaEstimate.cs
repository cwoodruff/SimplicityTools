using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Tca;

public sealed record TcaEstimate(
    SimplicitySnapshot Snapshot,
    IReadOnlyList<FilterEvaluation> FilterEvaluations,
    decimal EstimatedCost);
