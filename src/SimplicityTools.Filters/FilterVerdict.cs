namespace SimplicityTools.Filters;

/// <summary>
/// Describes the outcome of one Simplicity-First filter.
/// </summary>
/// <param name="Filter">The evaluated filter.</param>
/// <param name="Passes">Whether the filter passed.</param>
/// <param name="Score">The composite score for the filter.</param>
/// <param name="Summary">A short summary of the result.</param>
/// <param name="SubScores">The named sub-scores that contributed to the composite score.</param>
/// <param name="Violations">The reasons the filter did not achieve a perfect score.</param>
/// <param name="Recommendations">The highest-priority recommendation for improving the filter.</param>
public sealed record FilterVerdict(
    FilterName Filter,
    bool Passes,
    double Score,
    string Summary,
    IReadOnlyList<FilterSubScore> SubScores,
    IReadOnlyList<string> Violations,
    IReadOnlyList<string> Recommendations);
