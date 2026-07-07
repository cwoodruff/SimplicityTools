using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

internal static class FilterScoring
{
    /// <summary>
    /// A sub-score below this floor marks a collapsed dimension: the verdict cannot pass no
    /// matter how well the other dimensions average out.
    /// </summary>
    internal const double CatastrophicSubScoreFloor = 0.1;

    internal static FilterVerdict CreateVerdict(
        FilterName filter,
        double passingScore,
        IReadOnlyList<FilterSubScore> subScores,
        IReadOnlyDictionary<string, string> violationMessages,
        IReadOnlyDictionary<string, string> recommendationMessages)
    {
        var boundedSubScores = subScores
            .Select(subScore => subScore with { Score = Clamp(subScore.Score) })
            .ToArray();
        var score = boundedSubScores.Average(static subScore => subScore.Score);

        // Violations and recommendations describe sub-scores that miss the passing bar, not
        // any sub-score short of perfection.
        var failingSubScores = boundedSubScores.Where(subScore => subScore.Score < passingScore).ToArray();
        var lowestScore = failingSubScores
            .OrderBy(static subScore => subScore.Score)
            .FirstOrDefault();

        var hasCollapsedDimension = boundedSubScores.Any(static subScore => subScore.Score < CatastrophicSubScoreFloor);
        var passes = score >= passingScore && !hasCollapsedDimension;

        return new FilterVerdict(
            filter,
            passes,
            score,
            CreateSummary(filter, score, passingScore, boundedSubScores, hasCollapsedDimension),
            boundedSubScores,
            failingSubScores
                .Select(subScore => violationMessages[subScore.Name])
                .Take(5)
                .ToArray(),
            lowestScore is null
                ? []
                : [recommendationMessages[lowestScore.Name]]);
    }

    /// <summary>
    /// The failing verdict every evaluator returns for a snapshot with no measurable code —
    /// a failed collection must never be reported as an ideally simple codebase.
    /// </summary>
    internal static FilterVerdict CreateEmptySnapshotVerdict(FilterName filter)
    {
        return new FilterVerdict(
            filter,
            Passes: false,
            Score: 0.0,
            $"{filter} fails: the snapshot contains no measurable code, so nothing can be scored.",
            SubScores: [],
            Violations: ["The snapshot contains no measurable code (zero projects or zero files) — the collection may have failed."],
            Recommendations: ["Verify the solution path and check collection diagnostics before trusting this run."]);
    }

    internal static bool HasMeasurableCode(SimplicitySnapshot snapshot) =>
        snapshot.TotalProjects > 0 && snapshot.TotalFiles > 0;

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

    internal static double PrimaryPathConcentration(SimplicitySnapshot snapshot, double ratioTarget)
        => Clamp(snapshot.PrimaryPathRatio / ratioTarget);

    internal static double PrematureAbstraction(double ratio, double ratioTarget)
        => Clamp(2.0 - (ratio / ratioTarget));

    internal static double UnusedDependencyAccumulation(int count)
        => count <= 0 ? 1.0 : Clamp(1.0 - (count * 0.1));

    internal static double Clamp(double value)
        => double.IsNaN(value) ? 0.0 : Math.Clamp(value, 0.0, 1.0);

    private static string CreateSummary(
        FilterName filter,
        double score,
        double passingScore,
        IReadOnlyCollection<FilterSubScore> subScores,
        bool hasCollapsedDimension)
    {
        var metCheckCount = subScores.Count(subScore => subScore.Score >= passingScore);
        var outcome = score >= passingScore && !hasCollapsedDimension ? "passes" : "fails";
        var collapsedNote = hasCollapsedDimension && score >= passingScore
            ? " A collapsed dimension blocks the pass despite the average."
            : string.Empty;

        return $"{filter} {outcome} with score {score:F2} ({metCheckCount}/{subScores.Count} checks at or above {passingScore:F2}).{collapsedNote}";
    }
}
