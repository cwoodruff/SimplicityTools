using System.Text.RegularExpressions;
using Microsoft.Build.Construction;

namespace SimplicityTools.Metrics;

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

        var totalProjects = 0;
        var totalFiles = 0;
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in solution.ProjectsInOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsCSharpProject(project))
            {
                continue;
            }

            totalProjects++;

            var projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, project.RelativePath));
            if (!File.Exists(projectPath))
            {
                // Declared but missing on disk: still counted as a project so the collector can
                // report the workspace-load shortfall; there are no files to scan.
                continue;
            }

            var projectMetrics = ScanProject(projectPath, cancellationToken);

            totalFiles += projectMetrics.SourceFileCount;
            packages.UnionWith(projectMetrics.PackageReferences);
        }

        return new StructuralMetrics(totalProjects, totalFiles, packages.Count);
    }

    private static bool IsCSharpProject(ProjectInSolution project)
    {
        return project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat &&
               string.Equals(Path.GetExtension(project.RelativePath), ".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static ProjectMetrics ScanProject(string projectPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file '{projectPath}' was not found.", projectPath);
        }

        var projectRoot = ProjectRootElement.Open(projectPath)
            ?? throw new InvalidOperationException($"Could not load project '{projectPath}'.");
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Could not resolve the directory for '{projectPath}'.");

        var candidateFiles = EnumerateCandidateFiles(projectDirectory).ToArray();
        var includedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (IsDefaultCompileItemsEnabled(projectRoot))
        {
            includedFiles.UnionWith(candidateFiles);
        }

        foreach (var item in projectRoot.Items.Where(item => string.Equals(item.ItemType, "Compile", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(item.Include))
            {
                includedFiles.UnionWith(ExpandFiles(item.Include, item.Exclude, projectDirectory, candidateFiles));
            }

            if (!string.IsNullOrWhiteSpace(item.Remove))
            {
                includedFiles.ExceptWith(ExpandFiles(item.Remove, null, projectDirectory, candidateFiles));
            }
        }

        var packageReferences = projectRoot.Items
            .Where(item => string.Equals(item.ItemType, "PackageReference", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Include)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Trim())
            .ToArray();

        return new ProjectMetrics(includedFiles.Count, packageReferences);
    }

    private static bool IsDefaultCompileItemsEnabled(ProjectRootElement projectRoot)
    {
        var property = projectRoot.Properties
            .LastOrDefault(property => string.Equals(property.Name, "EnableDefaultCompileItems", StringComparison.OrdinalIgnoreCase));

        return property is null || !string.Equals(property.Value?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExpandFiles(
        string includes,
        string? excludes,
        string projectDirectory,
        IReadOnlyList<string> candidateFiles)
    {
        var excludedPaths = ExpandPatterns(excludes, projectDirectory, candidateFiles).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in ExpandPatterns(includes, projectDirectory, candidateFiles))
        {
            if (!excludedPaths.Contains(path))
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> ExpandPatterns(
        string? patterns,
        string projectDirectory,
        IReadOnlyList<string> candidateFiles)
    {
        if (string.IsNullOrWhiteSpace(patterns))
        {
            yield break;
        }

        foreach (var rawPattern in patterns.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawPattern.Contains("$(", StringComparison.Ordinal))
            {
                continue;
            }

            var pattern = NormalizePath(rawPattern);

            if (ContainsWildcard(pattern))
            {
                var matcher = CreateGlobRegex(pattern);
                foreach (var candidate in candidateFiles)
                {
                    var relativePath = NormalizePath(Path.GetRelativePath(projectDirectory, candidate));
                    if (matcher.IsMatch(relativePath))
                    {
                        yield return candidate;
                    }
                }

                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, pattern));
            if (File.Exists(fullPath) && SourceFileConventions.IsCountableSourceFile(fullPath))
            {
                yield return fullPath;
            }
        }
    }

    private static IEnumerable<string> EnumerateCandidateFiles(string projectDirectory)
    {
        foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (SourceFileConventions.IsCountableSourceFile(file))
            {
                yield return file;
            }
        }
    }

    private static bool ContainsWildcard(string pattern)
    {
        return pattern.Contains('*', StringComparison.Ordinal) || pattern.Contains('?', StringComparison.Ordinal);
    }

    private static Regex CreateGlobRegex(string pattern)
    {
        var regexPattern = "^" +
                           Regex.Escape(pattern)
                               .Replace(@"\*\*", "__DOUBLE_STAR__", StringComparison.Ordinal)
                               .Replace(@"\*", @"[^/]*", StringComparison.Ordinal)
                               .Replace(@"\?", @"[^/]", StringComparison.Ordinal)
                               .Replace("__DOUBLE_STAR__", @".*", StringComparison.Ordinal) +
                           "$";

        return new Regex(regexPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    internal readonly record struct StructuralMetrics(
        int TotalProjects,
        int TotalFiles,
        int ExternalDependencyCount);

    private readonly record struct ProjectMetrics(
        int SourceFileCount,
        IReadOnlyList<string> PackageReferences);
}
