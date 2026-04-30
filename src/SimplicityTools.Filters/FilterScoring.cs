using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

internal static class FilterScoring
{
    internal const double PassThreshold = 0.7;

    internal static FilterVerdict CreateVerdict(
        FilterName filter,
        FilterSubScore[] subScores,
        IReadOnlyDictionary<string, string> violationMessages,
        IReadOnlyDictionary<string, string> recommendationMessages)
    {
        var boundedSubScores = subScores
            .Select(subScore => subScore with { Score = Clamp(subScore.Score) })
            .ToArray();
        var score = boundedSubScores.Average(static subScore => subScore.Score);
        var failingSubScores = boundedSubScores.Where(static subScore => subScore.Score < 1.0).ToArray();
        var lowestScore = failingSubScores
            .OrderBy(static subScore => subScore.Score)
            .FirstOrDefault();

        return new FilterVerdict(
            filter,
            score >= PassThreshold,
            score,
            CreateSummary(filter, score, boundedSubScores),
            boundedSubScores,
            failingSubScores
                .Select(subScore => violationMessages[subScore.Name])
                .Take(5)
                .ToArray(),
            lowestScore is null
                ? []
                : [recommendationMessages[lowestScore.Name]]);
    }

    internal static double InverseThreshold(double metric, double target)
    {
        if (metric <= 0)
        {
            return 1.0;
        }

        return Clamp(target / metric);
    }

    internal static double RatioThreshold(int numerator, int denominator, double maxRatio)
    {
        if (numerator <= 0)
        {
            return 1.0;
        }

        if (denominator <= 0)
        {
            return 0.0;
        }

        return Clamp(maxRatio / ((double)numerator / denominator));
    }

    internal static double PrimaryPathConcentration(SimplicitySnapshot snapshot)
        => Clamp(snapshot.PrimaryPathRatio / 0.60);

    internal static double PrematureAbstraction(double ratio)
        => Clamp(2.0 - (ratio / 0.25));

    internal static double UnusedDependencyAccumulation(int count)
        => count <= 0 ? 1.0 : Clamp(1.0 - (count * 0.1));

    internal static double Clamp(double value)
        => double.IsNaN(value) ? 0.0 : Math.Clamp(value, 0.0, 1.0);

    private static string CreateSummary(FilterName filter, double score, IReadOnlyCollection<FilterSubScore> subScores)
    {
        var metCheckCount = subScores.Count(subScore => subScore.Score >= PassThreshold);
        var outcome = score >= PassThreshold ? "passes" : "fails";

        return $"{filter} {outcome} with score {score:F2} ({metCheckCount}/{subScores.Count} checks at or above 0.70).";
    }
}
