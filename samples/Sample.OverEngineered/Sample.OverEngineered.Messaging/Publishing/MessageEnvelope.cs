namespace Sample.OverEngineered.Messaging;

public sealed record MessageEnvelope(string Topic, string Payload, DateTimeOffset OccurredAt, string CorrelationId);
