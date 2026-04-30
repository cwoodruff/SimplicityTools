using Sample.OverEngineered.Infrastructure.Correlation;
using Sample.OverEngineered.Infrastructure.Time;

namespace Sample.OverEngineered.Messaging;

public sealed class EnvelopeFactory : IEnvelopeFactory
{
    private readonly ISystemClock _systemClock;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public EnvelopeFactory(ISystemClock systemClock, ICorrelationIdAccessor correlationIdAccessor)
    {
        _systemClock = systemClock;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public MessageEnvelope Create(string topic, string payload)
    {
        return new MessageEnvelope(topic, payload, _systemClock.UtcNow, _correlationIdAccessor.GetCurrentCorrelationId());
    }
}
