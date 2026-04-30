using Sample.OverEngineered.Infrastructure.Correlation;

namespace Sample.OverEngineered.Cache;

public sealed class CacheKeyBuilder : ICacheKeyBuilder
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CacheKeyBuilder(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public string Build(string orderNumber) => $"{_correlationIdAccessor.GetCurrentCorrelationId()}::{orderNumber}";
}
