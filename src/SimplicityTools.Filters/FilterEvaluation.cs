using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

public sealed record FilterEvaluation(
    string FilterName,
    bool Passed,
    string Summary,
    SimplicitySnapshot Snapshot);
