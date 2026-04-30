namespace SimplicityTools.Filters;

public sealed record FilterVerdict(
    FilterName Filter,
    bool Passes,
    double Score,
    string Summary,
    FilterSubScore[] SubScores,
    string[] Violations,
    string[] Recommendations);
