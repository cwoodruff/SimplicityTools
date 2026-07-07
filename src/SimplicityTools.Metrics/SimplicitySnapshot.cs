using System.Globalization;
using System.Text.Json.Serialization;

namespace SimplicityTools.Metrics;

/// <summary>
/// Immutable summary of the simplicity signals collected for a solution.
/// </summary>
public sealed record SimplicitySnapshot
{
    /// <summary>
    /// Gets the number of projects in the solution.
    /// </summary>
    public required int TotalProjects { get; init; }

    /// <summary>
    /// Gets the number of countable source files in the solution.
    /// </summary>
    public required int TotalFiles { get; init; }

    /// <summary>
    /// Gets the number of files identified as part of the primary path.
    /// </summary>
    public required int PrimaryPathFileCount { get; init; }

    /// <summary>
    /// Gets the number of interface types declared in the solution, used as the proxy for
    /// abstraction layers.
    /// </summary>
    public required int AbstractionLayerCount { get; init; }

    /// <summary>
    /// Gets the number of external package dependencies referenced by the solution.
    /// </summary>
    public required int ExternalDependencyCount { get; init; }

    /// <summary>
    /// Gets the number of external package dependencies that appear unused.
    /// </summary>
    public required int UnusedDependencyCount { get; init; }

    /// <summary>
    /// Gets the number of interfaces with a single implementation.
    /// </summary>
    public required int InterfacesWithSingleImplementation { get; init; }

    /// <summary>
    /// Gets the average cyclomatic complexity across measured methods.
    /// </summary>
    public required double AverageMethodComplexity { get; init; }

    /// <summary>
    /// Gets the estimated onboarding time for understanding the solution, or null when the
    /// metric has not been computed.
    /// </summary>
    public required TimeSpan? EstimatedOnboardingTime { get; init; }

    /// <summary>
    /// Gets the timestamp when the snapshot was collected.
    /// </summary>
    public required DateTimeOffset CollectedAt { get; init; }

    /// <summary>
    /// Gets the fraction of counted files that belong to the primary path.
    /// </summary>
    [JsonIgnore]
    public double PrimaryPathRatio =>
        TotalFiles > 0 ? (double)PrimaryPathFileCount / TotalFiles : 0;

    /// <summary>
    /// Gets the fraction of abstraction layers represented by single-implementation interfaces.
    /// </summary>
    [JsonIgnore]
    public double PrematureAbstractionRatio =>
        AbstractionLayerCount > 0
            ? (double)InterfacesWithSingleImplementation / AbstractionLayerCount
            : InterfacesWithSingleImplementation > 0 ? 1.0 : 0.0;

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
    /// <returns>An empty snapshot with zeroed metrics and a current timestamp.</returns>
    public static SimplicitySnapshot Empty() =>
        new()
        {
            TotalProjects = 0,
            TotalFiles = 0,
            PrimaryPathFileCount = 0,
            AbstractionLayerCount = 0,
            ExternalDependencyCount = 0,
            UnusedDependencyCount = 0,
            InterfacesWithSingleImplementation = 0,
            AverageMethodComplexity = 0,
            EstimatedOnboardingTime = null,
            CollectedAt = DateTimeOffset.UtcNow
        };
}
