namespace Sample.OverEngineered.Domain.Contracts;

public sealed record PlaceOrderInput(string CustomerId, IReadOnlyList<PlaceOrderLineInput> Lines, string Notes);
