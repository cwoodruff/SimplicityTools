using System.Threading;

namespace Sample.OverEngineered.Domain.Services;

public sealed class SequentialOrderNumberGenerator : IOrderNumberGenerator
{
    private int _seed = 1000;

    public string Generate()
    {
        var nextValue = Interlocked.Increment(ref _seed);
        return $"ORD-{nextValue:D5}";
    }
}
