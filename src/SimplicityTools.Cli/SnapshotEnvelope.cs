using System.Text.Json;
using SimplicityTools.Metrics;

namespace SimplicityTools.Cli;

/// <summary>
/// Versioned wrapper for persisted snapshots. Reading validates the schema explicitly so a file
/// written by a different tool version fails loudly instead of zero-filling drifted properties
/// and fabricating regressions.
/// </summary>
internal sealed record SnapshotEnvelope(int Version, string ToolVersion, SimplicitySnapshot Snapshot)
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly string[] RequiredSnapshotProperties =
    [
        "totalProjects",
        "totalFiles",
        "primaryPathFileCount",
        "abstractionLayerCount",
        "externalDependencyCount",
        "unusedDependencyCount",
        "interfacesWithSingleImplementation",
        "averageMethodComplexity",
        "estimatedOnboardingTime",
        "collectedAt"
    ];

    // Older tool versions serialized these computed ratios alongside the measured values.
    private static readonly HashSet<string> DerivedSnapshotProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "primaryPathRatio",
        "prematureAbstractionRatio"
    };

    public static string Serialize(SimplicitySnapshot snapshot)
    {
        var envelope = new SnapshotEnvelope(CurrentVersion, GetToolVersion(), snapshot);
        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    /// <summary>
    /// Reads a snapshot from either the versioned envelope format or a legacy raw-snapshot file.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The envelope version is unsupported, or the snapshot payload is missing required
    /// properties or carries unknown ones.
    /// </exception>
    public static SimplicitySnapshot Deserialize(string json, string sourceDescription)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{sourceDescription} did not contain a JSON object.");
        }

        if (root.TryGetProperty("version", out var versionElement))
        {
            if (versionElement.ValueKind != JsonValueKind.Number || versionElement.GetInt32() != CurrentVersion)
            {
                throw new InvalidOperationException(
                    $"{sourceDescription} uses snapshot schema version '{versionElement.ToString()}', but this tool only supports version {CurrentVersion}. Re-create it with this tool version.");
            }

            if (!root.TryGetProperty("snapshot", out var snapshotElement) || snapshotElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"{sourceDescription} is missing its 'snapshot' payload.");
            }

            return ReadSnapshot(snapshotElement, sourceDescription);
        }

        // Legacy pre-envelope files serialized the raw snapshot at the root.
        return ReadSnapshot(root, sourceDescription);
    }

    private static SimplicitySnapshot ReadSnapshot(JsonElement snapshotElement, string sourceDescription)
    {
        var presentProperties = snapshotElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingProperties = RequiredSnapshotProperties
            .Where(required => !presentProperties.Contains(required))
            .ToArray();
        if (missingProperties.Length > 0)
        {
            throw new InvalidOperationException(
                $"{sourceDescription} is missing snapshot propert{(missingProperties.Length == 1 ? "y" : "ies")} '{string.Join("', '", missingProperties)}' — it was likely written by an incompatible tool version. Re-create it with this tool version.");
        }

        var unknownProperties = presentProperties
            .Where(present => !RequiredSnapshotProperties.Contains(present, StringComparer.OrdinalIgnoreCase))
            .Where(present => !DerivedSnapshotProperties.Contains(present))
            .ToArray();
        if (unknownProperties.Length > 0)
        {
            throw new InvalidOperationException(
                $"{sourceDescription} contains unknown snapshot propert{(unknownProperties.Length == 1 ? "y" : "ies")} '{string.Join("', '", unknownProperties)}' — it was likely written by a newer tool version. Re-create it with this tool version.");
        }

        return snapshotElement.Deserialize<SimplicitySnapshot>(SerializerOptions)
            ?? throw new InvalidOperationException($"{sourceDescription} did not contain a valid snapshot.");
    }

    private static string GetToolVersion()
    {
        return typeof(SnapshotEnvelope).Assembly.GetName().Version?.ToString(3) ?? "unknown";
    }
}
