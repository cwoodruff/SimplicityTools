namespace Sample.OverEngineered.Domain.Contracts;

public sealed record PlaceOrderLineInput(string Sku, int Quantity, decimal UnitPrice);
