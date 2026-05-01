using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

/// <summary>
/// Captures the result of evaluating a single teaching filter against a snapshot.
/// </summary>
/// <param name="FilterName">The display name of the evaluated filter.</param>
/// <param name="Passed">Whether the filter passed.</param>
/// <param name="Summary">A short summary of the outcome.</param>
/// <param name="Snapshot">The snapshot used for the evaluation.</param>
public sealed record FilterEvaluation(
    string FilterName,
    bool Passed,
    string Summary,
    SimplicitySnapshot Snapshot);
