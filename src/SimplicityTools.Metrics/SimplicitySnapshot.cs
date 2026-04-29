namespace SimplicityTools.Metrics;

public sealed record SimplicitySnapshot(
    string SolutionName,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, decimal> Metrics)
{
    public static SimplicitySnapshot Empty(string solutionName) =>
        new(solutionName, DateTimeOffset.UtcNow, new Dictionary<string, decimal>());
}
