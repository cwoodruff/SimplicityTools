namespace Sample.OverEngineered.Telemetry;

public sealed class ConsoleTelemetrySink : ITelemetrySink
{
    public void Track(string eventName, string message)
    {
        Console.WriteLine($"[telemetry] {eventName}: {message}");
    }
}
