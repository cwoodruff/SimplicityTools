namespace Sample.OverEngineered.Telemetry;

public interface IActivityScopeFactory
{
    IDisposable Create(string activityName);
}
