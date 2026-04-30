using System.Text.Json;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal static class BaselineSnapshotFile
{
    private const string FileName = ".simplicity-baseline.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

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
        var json = JsonSerializer.Serialize(snapshot, SerializerOptions);
        await File.WriteAllTextAsync(baselinePath, json, cancellationToken).ConfigureAwait(false);
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
            await using var stream = File.OpenRead(baselinePath);
            return await JsonSerializer.DeserializeAsync<SimplicitySnapshot>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Baseline file '{baselinePath}' did not contain a valid snapshot.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Baseline file '{baselinePath}' is not valid JSON.", exception);
        }
    }
}
