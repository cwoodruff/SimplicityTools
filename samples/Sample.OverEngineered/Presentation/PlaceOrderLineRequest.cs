namespace Sample.OverEngineered.Presentation;

internal sealed record PlaceOrderLineRequest(string Sku, int Quantity, decimal UnitPrice);
