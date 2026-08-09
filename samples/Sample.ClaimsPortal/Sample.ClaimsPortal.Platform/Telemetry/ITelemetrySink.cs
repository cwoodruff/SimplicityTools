namespace Sample.ClaimsPortal.Platform.Telemetry;

/// <summary>
/// SF0001 hit: one implementation, <see cref="ConsoleTelemetrySink" />.
/// </summary>
public interface ITelemetrySink
{
    void Record(string name, IReadOnlyDictionary<string, string> tags);
}
