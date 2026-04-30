namespace SimplicityTools.Analyzers;

internal static class AnalyzerSourceFileConventions
{
    public static bool IsCountableSourceFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) &&
               !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
               !ContainsDirectorySegment(path, "bin") &&
               !ContainsDirectorySegment(path, "obj");
    }

    public static bool ContainsDirectorySegment(string path, string segment)
    {
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
    }
}
