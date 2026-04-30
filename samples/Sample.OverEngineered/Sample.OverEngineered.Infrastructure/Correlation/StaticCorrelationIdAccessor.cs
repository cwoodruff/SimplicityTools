namespace Sample.OverEngineered.Infrastructure.Correlation;

public sealed class StaticCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly string _correlationId;

    public StaticCorrelationIdAccessor(string correlationId)
    {
        _correlationId = correlationId;
    }

    public string GetCurrentCorrelationId() => _correlationId;
}
