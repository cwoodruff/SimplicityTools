namespace SimplicityTools.Metrics;

/// <summary>
/// Collects a <see cref="SimplicitySnapshot" /> for a .NET solution.
/// </summary>
public interface ISimplicityCollector
{
    /// <summary>
    /// Analyzes the supplied solution and returns a snapshot of the measured simplicity signals.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file to analyze.</param>
    /// <param name="cancellationToken">A token used to cancel collection.</param>
    /// <returns>The collected snapshot.</returns>
    Task<SimplicitySnapshot> CollectAsync(string solutionPath, CancellationToken cancellationToken = default);
}
