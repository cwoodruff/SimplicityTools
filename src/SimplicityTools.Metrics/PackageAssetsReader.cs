using System.Text.Json;

namespace SimplicityTools.Metrics;

/// <summary>
/// Reads the authoritative package graph for a project from <c>obj/project.assets.json</c>:
/// which packages are directly declared and which assemblies each package contributes.
/// </summary>
internal static class PackageAssetsReader
{
    public static PackageAssets? TryRead(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return null;
        }

        var assetsPath = GetAssetsPath(projectPath);
        if (assetsPath is null || !File.Exists(assetsPath))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
            return Read(document.RootElement);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static string? GetAssetsPath(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        return projectDirectory is null ? null : Path.Combine(projectDirectory, "obj", "project.assets.json");
    }

    private static PackageAssets Read(JsonElement root)
    {
        var declaredPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("project", out var project) &&
            project.TryGetProperty("frameworks", out var frameworks))
        {
            foreach (var framework in frameworks.EnumerateObject())
            {
                if (!framework.Value.TryGetProperty("dependencies", out var dependencies))
                {
                    continue;
                }

                foreach (var dependency in dependencies.EnumerateObject())
                {
                    var autoReferenced = dependency.Value.TryGetProperty("autoReferenced", out var flag) &&
                                         flag.ValueKind == JsonValueKind.True;
                    if (!autoReferenced)
                    {
                        declaredPackageIds.Add(dependency.Name);
                    }
                }
            }
        }

        var assemblyNamesByPackageId = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        var buildOnlyPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("targets", out var targets))
        {
            foreach (var target in targets.EnumerateObject())
            {
                foreach (var entry in target.Value.EnumerateObject())
                {
                    var packageId = entry.Name.Split('/')[0];
                    if (!declaredPackageIds.Contains(packageId))
                    {
                        continue;
                    }

                    var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    CollectAssemblyNames(entry.Value, "compile", assemblyNames);
                    CollectAssemblyNames(entry.Value, "runtime", assemblyNames);

                    if (assemblyNames.Count > 0)
                    {
                        assemblyNamesByPackageId[packageId] = assemblyNames;
                    }
                }
            }
        }

        foreach (var packageId in declaredPackageIds)
        {
            if (!assemblyNamesByPackageId.ContainsKey(packageId))
            {
                buildOnlyPackageIds.Add(packageId);
            }
        }

        return new PackageAssets(declaredPackageIds, assemblyNamesByPackageId, buildOnlyPackageIds);
    }

    private static void CollectAssemblyNames(JsonElement packageEntry, string assetGroup, ISet<string> assemblyNames)
    {
        if (!packageEntry.TryGetProperty(assetGroup, out var assets))
        {
            return;
        }

        foreach (var asset in assets.EnumerateObject())
        {
            var fileName = Path.GetFileName(asset.Name);
            if (string.Equals(fileName, "_._", StringComparison.Ordinal) ||
                !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            assemblyNames.Add(Path.GetFileNameWithoutExtension(fileName));
        }
    }

    internal sealed record PackageAssets(
        IReadOnlySet<string> DeclaredPackageIds,
        IReadOnlyDictionary<string, IReadOnlySet<string>> AssemblyNamesByPackageId,
        IReadOnlySet<string> BuildOnlyPackageIds);
}
