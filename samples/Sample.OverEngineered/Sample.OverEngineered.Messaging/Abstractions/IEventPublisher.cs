namespace Sample.OverEngineered.Messaging;

public interface IEventPublisher
{
    void Publish(string topic, string payload);
}
