namespace Sample.OverEngineered.Messaging;

public interface IEnvelopeFactory
{
    MessageEnvelope Create(string topic, string payload);
}
