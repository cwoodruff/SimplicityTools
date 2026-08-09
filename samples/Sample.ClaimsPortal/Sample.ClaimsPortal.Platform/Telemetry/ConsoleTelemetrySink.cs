namespace Sample.ClaimsPortal.Platform.Telemetry;

public sealed class ConsoleTelemetrySink : ITelemetrySink
{
    private readonly List<string> _recorded = [];

    public IReadOnlyList<string> Recorded => _recorded;

    public void Record(string name, IReadOnlyDictionary<string, string> tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tags);

        var formattedTags = string.Join(", ", tags.Select(tag => $"{tag.Key}={tag.Value}"));
        _recorded.Add($"{name} [{formattedTags}]");
    }
}
