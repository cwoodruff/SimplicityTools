using System.Text.Json;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal static class SnapshotHistory
{
    private const string DirectoryName = ".simplicity-history";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string GetDirectoryPath(string solutionPath)
    {
        ArgumentNullException.ThrowIfNull(solutionPath);

        var fullSolutionPath = Path.GetFullPath(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath)
            ?? throw new InvalidOperationException($"Could not determine the directory for '{solutionPath}'.");

        return Path.Combine(solutionDirectory, DirectoryName);
    }

    public static async Task<IReadOnlyList<SimplicitySnapshot>> ReadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        var historyDirectory = GetDirectoryPath(solutionPath);
        if (!Directory.Exists(historyDirectory))
        {
            return [];
        }

        var snapshots = new List<SimplicitySnapshot>();

        foreach (var filePath in Directory.EnumerateFiles(historyDirectory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await using var stream = File.OpenRead(filePath);
                var snapshot = await JsonSerializer.DeserializeAsync<SimplicitySnapshot>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
                if (snapshot is not null)
                {
                    snapshots.Add(snapshot);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (JsonException)
            {
            }
        }

        return snapshots
            .OrderBy(static snapshot => snapshot.CollectedAt)
            .ToArray();
    }
}
