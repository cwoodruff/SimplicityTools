namespace Sample.OverEngineered.Infrastructure.Time;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
