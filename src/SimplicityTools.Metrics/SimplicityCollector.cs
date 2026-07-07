using Microsoft.CodeAnalysis.MSBuild;

namespace SimplicityTools.Metrics;

/// <summary>
/// Default implementation of <see cref="ISimplicityCollector" /> that combines the structural,
/// semantic, and heuristic collection passes over a single shared Roslyn workspace.
/// <para>
/// <b>Process-global side effects:</b> by default the first collection registers an MSBuild
/// instance via <c>Microsoft.Build.Locator</c> (which mutates assembly resolution for the whole
/// process) and collection may spawn a <c>dotnet restore</c> child process when the target
/// solution's package assets are missing or stale. Hosts that need to suppress either behavior
/// should use the <see cref="SimplicityCollector(SimplicityCollectorOptions, Action{string}?)" />
/// overload with <see cref="SimplicityCollectorOptions" />.
/// </para>
/// </summary>
public sealed class SimplicityCollector : ISimplicityCollector
{
    private readonly StructuralCollectionPass structuralCollectionPass;
    private readonly SemanticCollectionPass semanticCollectionPass;
    private readonly HeuristicCollectionPass heuristicCollectionPass;
    private readonly SimplicityCollectorOptions options;
    private readonly Action<string>? onDiagnostic;

    /// <summary>
    /// Creates a collector with the built-in collection passes.
    /// </summary>
    /// <param name="onDiagnostic">
    /// Optional sink for non-fatal collection diagnostics, such as projects that fail to load
    /// into the analysis workspace. When null, diagnostics are discarded.
    /// </param>
    public SimplicityCollector(Action<string>? onDiagnostic = null)
        : this(new SimplicityCollectorOptions(), onDiagnostic)
    {
    }

    /// <summary>
    /// Creates a collector with the built-in collection passes and explicit side-effect options.
    /// </summary>
    /// <param name="options">
    /// Opt-outs for the process-global side effects of collection: spawning <c>dotnet restore</c>
    /// and registering an MSBuild instance via <c>Microsoft.Build.Locator</c>. See
    /// <see cref="SimplicityCollectorOptions" /> for the consequences of each opt-out.
    /// </param>
    /// <param name="onDiagnostic">
    /// Optional sink for non-fatal collection diagnostics, such as projects that fail to load
    /// into the analysis workspace. When null, diagnostics are discarded.
    /// </param>
    public SimplicityCollector(SimplicityCollectorOptions options, Action<string>? onDiagnostic = null)
        : this(new StructuralCollectionPass(), new SemanticCollectionPass(), new HeuristicCollectionPass(), onDiagnostic, options)
    {
    }

    internal SimplicityCollector(
        StructuralCollectionPass structuralCollectionPass,
        SemanticCollectionPass semanticCollectionPass,
        HeuristicCollectionPass heuristicCollectionPass,
        Action<string>? onDiagnostic = null,
        SimplicityCollectorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(structuralCollectionPass);
        ArgumentNullException.ThrowIfNull(semanticCollectionPass);
        ArgumentNullException.ThrowIfNull(heuristicCollectionPass);
        this.structuralCollectionPass = structuralCollectionPass;
        this.semanticCollectionPass = semanticCollectionPass;
        this.heuristicCollectionPass = heuristicCollectionPass;
        this.options = options ?? new SimplicityCollectorOptions();
        this.onDiagnostic = onDiagnostic;
    }

    /// <inheritdoc />
    public async Task<SimplicitySnapshot> CollectAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        if (options.RegisterMSBuildLocator)
        {
            MSBuildLocatorRegistration.EnsureRegistered();
        }

        var collectedAt = DateTimeOffset.UtcNow;
        var structuralMetrics = structuralCollectionPass.Collect(solutionPath, cancellationToken);

        if (options.AllowRestore)
        {
            await SolutionRestoreCoordinator.RestoreIfNeededAsync(solutionPath, cancellationToken).ConfigureAwait(false);
        }

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
