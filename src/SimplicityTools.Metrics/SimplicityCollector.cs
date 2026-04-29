namespace SimplicityTools.Metrics;

public sealed class SimplicityCollector : ISimplicityCollector
{
    private readonly StructuralCollectionPass structuralCollectionPass;
    private readonly SemanticCollectionPass semanticCollectionPass;
    private readonly HeuristicCollectionPass heuristicCollectionPass;

    public SimplicityCollector()
        : this(new StructuralCollectionPass(), new SemanticCollectionPass(), new HeuristicCollectionPass())
    {
    }

    internal SimplicityCollector(
        StructuralCollectionPass structuralCollectionPass,
        SemanticCollectionPass semanticCollectionPass,
        HeuristicCollectionPass heuristicCollectionPass)
    {
        ArgumentNullException.ThrowIfNull(structuralCollectionPass);
        ArgumentNullException.ThrowIfNull(semanticCollectionPass);
        ArgumentNullException.ThrowIfNull(heuristicCollectionPass);
        this.structuralCollectionPass = structuralCollectionPass;
        this.semanticCollectionPass = semanticCollectionPass;
        this.heuristicCollectionPass = heuristicCollectionPass;
    }

    public async Task<SimplicitySnapshot> CollectAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        MSBuildLocatorRegistration.EnsureRegistered();

        var collectedAt = DateTimeOffset.UtcNow;
        var structuralMetrics = structuralCollectionPass.Collect(solutionPath, cancellationToken);
        var semanticMetrics = await semanticCollectionPass.CollectAsync(solutionPath, cancellationToken).ConfigureAwait(false);
        var primaryPathFileCount = await heuristicCollectionPass.CollectPrimaryPathFileCountAsync(solutionPath, cancellationToken).ConfigureAwait(false);

        return new SimplicitySnapshot(
            structuralMetrics.TotalProjects,
            structuralMetrics.TotalFiles,
            primaryPathFileCount,
            semanticMetrics.AbstractionLayerCount,
            semanticMetrics.ExternalDependencyCount,
            semanticMetrics.UnusedDependencyCount,
            semanticMetrics.InterfacesWithSingleImplementation,
            semanticMetrics.AverageMethodComplexity,
            TimeSpan.Zero,
            collectedAt);
    }
}
