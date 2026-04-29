using System.Globalization;

namespace SimplicityTools.Metrics;

public sealed record SimplicitySnapshot(
    int TotalProjects,
    int TotalFiles,
    int PrimaryPathFileCount,
    int AbstractionLayerCount,
    int ExternalDependencyCount,
    int UnusedDependencyCount,
    int InterfacesWithSingleImplementation,
    double AverageMethodComplexity,
    TimeSpan EstimatedOnboardingTime,
    DateTimeOffset CollectedAt)
{
    public double PrimaryPathRatio =>
        TotalFiles > 0 ? (double)PrimaryPathFileCount / TotalFiles : 0;

    public double PrematureAbstractionRatio =>
        AbstractionLayerCount > 0
            ? (double)InterfacesWithSingleImplementation / AbstractionLayerCount
            : 0;

    public string ToSummary()
    {
        var collectedDate = CollectedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var averageComplexity = AverageMethodComplexity.ToString("F1", CultureInfo.InvariantCulture);
        var onboardingHours = EstimatedOnboardingTime.TotalHours.ToString("F0", CultureInfo.InvariantCulture);

        return string.Join(
            Environment.NewLine,
            [
                $"Simplicity Snapshot ({collectedDate})",
                "----------------------------------------",
                $"Projects: {TotalProjects}",
                $"Total files: {TotalFiles}",
                $"Primary path files: {PrimaryPathFileCount}",
                $"Abstraction layers: {AbstractionLayerCount}",
                $"Single-impl interfaces: {InterfacesWithSingleImplementation}",
                $"External deps: {ExternalDependencyCount} ({UnusedDependencyCount} unused)",
                $"Avg complexity: {averageComplexity}",
                $"Est. onboarding: {onboardingHours}h"
            ]);
    }

    public static SimplicitySnapshot Empty(string solutionName) =>
        new(
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            TimeSpan.Zero,
            DateTimeOffset.UtcNow);
}
