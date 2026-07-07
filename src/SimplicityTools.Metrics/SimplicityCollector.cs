using Microsoft.CodeAnalysis.MSBuild;

namespace SimplicityTools.Metrics;

/// <summary>
/// Default implementation of <see cref="ISimplicityCollector" /> that combines the structural,
/// semantic, and heuristic collection passes over a single shared Roslyn workspace.
/// </summary>
public sealed class SimplicityCollector : ISimplicityCollector
{
    private readonly StructuralCollectionPass structuralCollectionPass;
    private readonly SemanticCollectionPass semanticCollectionPass;
    private readonly HeuristicCollectionPass heuristicCollectionPass;
    private readonly Action<string>? onDiagnostic;

    /// <summary>
    /// Creates a collector with the built-in collection passes.
    /// </summary>
    /// <param name="onDiagnostic">
    /// Optional sink for non-fatal collection diagnostics, such as projects that fail to load
    /// into the analysis workspace. When null, diagnostics are discarded.
    /// </param>
    public SimplicityCollector(Action<string>? onDiagnostic = null)
        : this(new StructuralCollectionPass(), new SemanticCollectionPass(), new HeuristicCollectionPass(), onDiagnostic)
    {
    }

    internal SimplicityCollector(
        StructuralCollectionPass structuralCollectionPass,
        SemanticCollectionPass semanticCollectionPass,
        HeuristicCollectionPass heuristicCollectionPass,
        Action<string>? onDiagnostic = null)
    {
        ArgumentNullException.ThrowIfNull(structuralCollectionPass);
        ArgumentNullException.ThrowIfNull(semanticCollectionPass);
        ArgumentNullException.ThrowIfNull(heuristicCollectionPass);
        this.structuralCollectionPass = structuralCollectionPass;
        this.semanticCollectionPass = semanticCollectionPass;
        this.heuristicCollectionPass = heuristicCollectionPass;
        this.onDiagnostic = onDiagnostic;
    }

    /// <inheritdoc />
    public async Task<SimplicitySnapshot> CollectAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        MSBuildLocatorRegistration.EnsureRegistered();

        var collectedAt = DateTimeOffset.UtcNow;
        var structuralMetrics = structuralCollectionPass.Collect(solutionPath, cancellationToken);

        await SolutionRestoreCoordinator.RestoreIfNeededAsync(solutionPath, cancellationToken).ConfigureAwait(false);

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, args) =>
            onDiagnostic?.Invoke($"Workspace load {args.Diagnostic.Kind}: {args.Diagnostic.Message}");

        var solution = await workspace.OpenSolutionAsync(Path.GetFullPath(solutionPath), cancellationToken: cancellationToken).ConfigureAwait(false);

        var loadedProjectCount = solution.Projects
            .Select(project => project.FilePath)
            .Where(filePath => !string.IsNullOrWhiteSpace(filePath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (loadedProjectCount < structuralMetrics.TotalProjects)
        {
            onDiagnostic?.Invoke(
                $"Only {loadedProjectCount} of {structuralMetrics.TotalProjects} projects loaded into the analysis workspace; semantic metrics may be incomplete.");
        }

        var semanticMetrics = await semanticCollectionPass.CollectAsync(solution, structuralMetrics.ProjectFilePaths, cancellationToken).ConfigureAwait(false);
        var heuristicMetrics = await heuristicCollectionPass.CollectAsync(solution, structuralMetrics.ProjectFilePaths, cancellationToken).ConfigureAwait(false);

        return new SimplicitySnapshot(
            structuralMetrics.TotalProjects,
            heuristicMetrics.AnalyzedFileCount,
            heuristicMetrics.PrimaryPathFileCount,
            semanticMetrics.AbstractionLayerCount,
            semanticMetrics.ExternalDependencyCount,
            semanticMetrics.UnusedDependencyCount,
            semanticMetrics.InterfacesWithSingleImplementation,
            semanticMetrics.AverageMethodComplexity,
            null,
            collectedAt);
    }
}
