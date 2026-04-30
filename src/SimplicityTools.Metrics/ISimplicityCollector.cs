namespace SimplicityTools.Metrics;

public interface ISimplicityCollector
{
    Task<SimplicitySnapshot> CollectAsync(string solutionPath, CancellationToken cancellationToken = default);
}
