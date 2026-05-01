namespace SimplicityTools.Filters;

/// <summary>
/// Represents one named contribution to a filter verdict.
/// </summary>
/// <param name="Name">The name of the measured sub-score.</param>
/// <param name="Score">The normalized score for that sub-score.</param>
public sealed record FilterSubScore(string Name, double Score);
