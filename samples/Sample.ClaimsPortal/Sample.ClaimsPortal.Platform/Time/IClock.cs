namespace Sample.ClaimsPortal.Platform.Time;

/// <summary>
/// SF0001 hit: this interface has exactly one implementation in the solution
/// (<see cref="SystemClock" />). Tests fake the clock through a delegate instead, so the
/// interface buys nothing.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
