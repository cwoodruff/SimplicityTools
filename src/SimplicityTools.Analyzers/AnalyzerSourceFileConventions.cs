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

        var normalizedPath = path!;
        return !normalizedPath.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) &&
               !normalizedPath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
               !ContainsDirectorySegment(normalizedPath, "bin") &&
               !ContainsDirectorySegment(normalizedPath, "obj");
    }

    public static bool ContainsDirectorySegment(string path, string segment)
    {
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
    }
}
