using SimplicityTools.Metrics;

namespace SimplicityTools.Filters;

public static class PrimaryPathFirstEvaluator
{
    private const double MaxLayersPerPrimaryPathFile = 1.0 / 3.0;

    public static FilterVerdict Evaluate(SimplicitySnapshot snapshot)
    {
        var subScores = new[]
        {
            new FilterSubScore("Primary path concentration", FilterScoring.PrimaryPathConcentration(snapshot)),
            new FilterSubScore("Abstraction dilution", FilterScoring.RatioThreshold(snapshot.AbstractionLayerCount, snapshot.PrimaryPathFileCount, MaxLayersPerPrimaryPathFile)),
            new FilterSubScore("Project count", FilterScoring.InverseThreshold(snapshot.TotalProjects, 5.0))
        };

        return FilterScoring.CreateVerdict(
            FilterName.PrimaryPathFirst,
            subScores,
            new Dictionary<string, string>
            {
                ["Primary path concentration"] = "Too little of the codebase sits on the primary path to satisfy this filter.",
                ["Abstraction dilution"] = "Primary-path files are carrying too many abstraction layers.",
                ["Project count"] = "Project count is above the Primary Path First target."
            },
            new Dictionary<string, string>
            {
                ["Primary path concentration"] = "Move more business flow into the primary path and peel supporting concerns away from it.",
                ["Abstraction dilution"] = "Reduce abstraction layers around the primary path so important code stays direct.",
                ["Project count"] = "Merge or remove low-value projects until the solution shape is easier to follow."
            });
    }
}
