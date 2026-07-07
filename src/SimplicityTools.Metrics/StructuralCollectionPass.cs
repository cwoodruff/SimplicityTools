using Microsoft.Build.Construction;

namespace SimplicityTools.Metrics;

/// <summary>
/// Counts the C# projects declared by the solution file. File counting lives in
/// <see cref="HeuristicCollectionPass" /> so the primary-path numerator and the total-file
/// denominator are drawn from the same population.
/// </summary>
internal sealed class StructuralCollectionPass
{
    public StructuralMetrics Collect(string solutionPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);
        cancellationToken.ThrowIfCancellationRequested();

        var fullSolutionPath = Path.GetFullPath(solutionPath);
        var solution = SolutionFile.Parse(fullSolutionPath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath)
            ?? throw new InvalidOperationException($"Could not resolve the directory for '{solutionPath}'.");

        var projectFilePaths = solution.ProjectsInOrder
            .Where(IsCSharpProject)
            .Select(project => Path.GetFullPath(Path.Combine(solutionDirectory, project.RelativePath)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new StructuralMetrics(projectFilePaths.Count, projectFilePaths);
    }

    private static bool IsCSharpProject(ProjectInSolution project)
    {
        return project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat &&
               string.Equals(Path.GetExtension(project.RelativePath), ".csproj", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <paramref name="ProjectFilePaths" /> is the set of C# projects the solution declares.
    /// The analysis workspace also loads project-referenced projects from outside the solution;
    /// metrics must not include them.
    /// </summary>
    internal readonly record struct StructuralMetrics(int TotalProjects, IReadOnlySet<string> ProjectFilePaths);
}
