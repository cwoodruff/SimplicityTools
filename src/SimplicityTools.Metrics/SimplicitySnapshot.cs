using System.Globalization;

namespace SimplicityTools.Metrics;

/// <summary>
/// Immutable summary of the simplicity signals collected for a solution.
/// </summary>
/// <param name="TotalProjects">The number of projects in the solution.</param>
/// <param name="TotalFiles">The number of countable source files in the solution.</param>
/// <param name="PrimaryPathFileCount">The number of files identified as part of the primary path.</param>
/// <param name="AbstractionLayerCount">The number of abstraction layers detected in the solution.</param>
/// <param name="ExternalDependencyCount">The number of external package dependencies referenced by the solution.</param>
/// <param name="UnusedDependencyCount">The number of external package dependencies that appear unused.</param>
/// <param name="InterfacesWithSingleImplementation">The number of interfaces with a single implementation.</param>
/// <param name="AverageMethodComplexity">The average cyclomatic complexity across measured methods.</param>
/// <param name="EstimatedOnboardingTime">The estimated onboarding time for understanding the solution, or null when the metric has not been computed.</param>
/// <param name="CollectedAt">The timestamp when the snapshot was collected.</param>
public sealed record SimplicitySnapshot(
    int TotalProjects,
    int TotalFiles,
    int PrimaryPathFileCount,
    int AbstractionLayerCount,
    int ExternalDependencyCount,
    int UnusedDependencyCount,
    int InterfacesWithSingleImplementation,
    double AverageMethodComplexity,
    TimeSpan? EstimatedOnboardingTime,
    DateTimeOffset CollectedAt)
{
    /// <summary>
    /// Gets the fraction of counted files that belong to the primary path.
    /// </summary>
    public double PrimaryPathRatio =>
        TotalFiles > 0 ? (double)PrimaryPathFileCount / TotalFiles : 0;

    /// <summary>
    /// Gets the fraction of abstraction layers represented by single-implementation interfaces.
    /// </summary>
    public double PrematureAbstractionRatio =>
        AbstractionLayerCount > 0
            ? (double)InterfacesWithSingleImplementation / AbstractionLayerCount
            : 0;

    /// <summary>
    /// Formats the snapshot as the compact summary used by the CLI.
    /// </summary>
    /// <returns>A multi-line summary of the measured values.</returns>
    public string ToSummary()
    {
        var collectedDate = CollectedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var averageComplexity = AverageMethodComplexity.ToString("F1", CultureInfo.InvariantCulture);
        var onboarding = EstimatedOnboardingTime is { } measured
            ? measured.TotalHours.ToString("F0", CultureInfo.InvariantCulture) + "h"
            : "not computed";

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
                $"Est. onboarding: {onboarding}"
            ]);
    }

    /// <summary>
    /// Creates an empty snapshot placeholder.
    /// </summary>
    /// <param name="solutionName">Unused compatibility parameter retained for migration callers.</param>
    /// <returns>An empty snapshot with zeroed metrics and a current timestamp.</returns>
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
            null,
            DateTimeOffset.UtcNow);
}
