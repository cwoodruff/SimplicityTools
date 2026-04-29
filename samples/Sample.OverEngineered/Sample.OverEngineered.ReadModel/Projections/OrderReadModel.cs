namespace Sample.OverEngineered.ReadModel;

public sealed record OrderReadModel(string OrderNumber, string CustomerId, decimal Total, string Status);
