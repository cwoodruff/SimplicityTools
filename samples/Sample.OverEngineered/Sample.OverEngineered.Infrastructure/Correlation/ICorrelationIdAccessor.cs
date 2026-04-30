namespace Sample.OverEngineered.Infrastructure.Correlation;

public interface ICorrelationIdAccessor
{
    string GetCurrentCorrelationId();
}
