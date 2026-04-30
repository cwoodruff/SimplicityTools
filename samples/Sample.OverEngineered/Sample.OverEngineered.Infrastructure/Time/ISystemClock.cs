namespace Sample.OverEngineered.Infrastructure.Time;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
