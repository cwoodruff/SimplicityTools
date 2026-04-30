namespace Sample.OverEngineered.Messaging;

public sealed class EventPublisher : IEventPublisher
{
    private readonly IEnvelopeFactory _envelopeFactory;

    public EventPublisher(IEnvelopeFactory envelopeFactory)
    {
        _envelopeFactory = envelopeFactory;
    }

    public MessageEnvelope? LastEnvelope { get; private set; }

    public void Publish(string topic, string payload)
    {
        LastEnvelope = _envelopeFactory.Create(topic, payload);
    }
}
