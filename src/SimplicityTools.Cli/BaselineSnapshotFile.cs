using System.Text.Json;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal static class BaselineSnapshotFile
{
    private const string FileName = ".simplicity-baseline.json";

    public static string GetPath(string solutionPath)
    {
        ArgumentNullException.ThrowIfNull(solutionPath);

        var fullSolutionPath = Path.GetFullPath(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath)
            ?? throw new InvalidOperationException($"Could not determine the directory for '{solutionPath}'.");

        return Path.Combine(solutionDirectory, FileName);
    }

    public static async Task<string> WriteAsync(string solutionPath, SimplicitySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var baselinePath = GetPath(solutionPath);
        await File.WriteAllTextAsync(baselinePath, SnapshotEnvelope.Serialize(snapshot), cancellationToken).ConfigureAwait(false);
        return baselinePath;
    }

    public static async Task<SimplicitySnapshot> ReadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        var baselinePath = GetPath(solutionPath);

        if (!File.Exists(baselinePath))
        {
            throw new FileNotFoundException(
                $"Baseline file was not found at '{baselinePath}'. Run 'dotnet simplicity baseline <solution.sln>' first.",
                baselinePath);
        }

        try
        {
            var json = await File.ReadAllTextAsync(baselinePath, cancellationToken).ConfigureAwait(false);
            return SnapshotEnvelope.Deserialize(json, $"Baseline file '{baselinePath}'");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Baseline file '{baselinePath}' is not valid JSON.", exception);
        }
    }
}
