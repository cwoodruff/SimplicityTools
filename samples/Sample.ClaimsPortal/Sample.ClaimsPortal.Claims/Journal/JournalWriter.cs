namespace Sample.ClaimsPortal.Claims.Journal;

/// <summary>Leaf of the intake call chain measured by SF0004.</summary>
public sealed class JournalWriter
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Write(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);
        _lines.Add(line);
    }
}
