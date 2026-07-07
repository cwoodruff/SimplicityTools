using System.Globalization;
using System.Text.Json;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

internal static class SnapshotHistory
{
    private const string DirectoryName = ".simplicity-history";

    public const int DefaultRetentionLimit = 30;

    public static string GetDirectoryPath(string solutionPath)
    {
        ArgumentNullException.ThrowIfNull(solutionPath);

        var fullSolutionPath = Path.GetFullPath(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath)
            ?? throw new InvalidOperationException($"Could not determine the directory for '{solutionPath}'.");

        return Path.Combine(solutionDirectory, DirectoryName);
    }

    /// <summary>
    /// Appends the snapshot to <c>.simplicity-history/</c> as a timestamped envelope file and
    /// prunes the oldest entries beyond <paramref name="retentionLimit" />.
    /// </summary>
    public static async Task<string> AppendAsync(
        string solutionPath,
        SimplicitySnapshot snapshot,
        int retentionLimit = DefaultRetentionLimit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfLessThan(retentionLimit, 1);

        var historyDirectory = GetDirectoryPath(solutionPath);
        Directory.CreateDirectory(historyDirectory);

        var baseName = snapshot.CollectedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var filePath = Path.Combine(historyDirectory, baseName + ".json");
        for (var suffix = 2; File.Exists(filePath); suffix++)
        {
            filePath = Path.Combine(historyDirectory, $"{baseName}-{suffix}.json");
        }

        await File.WriteAllTextAsync(filePath, SnapshotEnvelope.Serialize(snapshot), cancellationToken).ConfigureAwait(false);

        // Timestamped names sort chronologically, so pruning by name drops the oldest runs.
        var existingFiles = Directory.GetFiles(historyDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var staleFile in existingFiles.Take(Math.Max(0, existingFiles.Length - retentionLimit)))
        {
            File.Delete(staleFile);
        }

        return filePath;
    }

    public static async Task<IReadOnlyList<SimplicitySnapshot>> ReadAsync(
        string solutionPath,
        TextWriter diagnosticsWriter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnosticsWriter);

        var historyDirectory = GetDirectoryPath(solutionPath);
        if (!Directory.Exists(historyDirectory))
        {
            return [];
        }

        var snapshots = new List<SimplicitySnapshot>();

        foreach (var filePath in Directory.EnumerateFiles(historyDirectory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                snapshots.Add(SnapshotEnvelope.Deserialize(json, $"History file '{filePath}'"));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                await diagnosticsWriter.WriteLineAsync(
                    $"Skipping unreadable history file '{Path.GetFileName(filePath)}': {exception.Message}").ConfigureAwait(false);
            }
        }

        return snapshots
            .OrderBy(static snapshot => snapshot.CollectedAt)
            .ToArray();
    }
}
