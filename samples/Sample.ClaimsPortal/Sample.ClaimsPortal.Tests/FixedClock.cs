using Sample.ClaimsPortal.Platform.Time;

namespace Sample.ClaimsPortal.Tests;

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; }

    public static FixedClock On(int year, int month, int day) =>
        new(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero));
}
