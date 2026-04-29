namespace Sample.OverEngineered.Telemetry;

public interface ITelemetrySink
{
    void Track(string eventName, string message);
}
