using Sample.OverEngineered.Infrastructure.Time;

namespace Sample.OverEngineered.Telemetry;

public sealed class ActivityScopeFactory : IActivityScopeFactory
{
    private readonly ISystemClock _systemClock;
    private readonly ITelemetrySink _telemetrySink;

    public ActivityScopeFactory(ISystemClock systemClock, ITelemetrySink telemetrySink)
    {
        _systemClock = systemClock;
        _telemetrySink = telemetrySink;
    }

    public IDisposable Create(string activityName)
    {
        _telemetrySink.Track("activity.start", $"{activityName}@{_systemClock.UtcNow:O}");
        return new ActivityScope(activityName, _systemClock, _telemetrySink);
    }

    private sealed class ActivityScope : IDisposable
    {
        private readonly string _activityName;
        private readonly ISystemClock _systemClock;
        private readonly ITelemetrySink _telemetrySink;

        public ActivityScope(string activityName, ISystemClock systemClock, ITelemetrySink telemetrySink)
        {
            _activityName = activityName;
            _systemClock = systemClock;
            _telemetrySink = telemetrySink;
        }

        public void Dispose()
        {
            _telemetrySink.Track("activity.stop", $"{_activityName}@{_systemClock.UtcNow:O}");
        }
    }
}
