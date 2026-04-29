namespace Sample.OverEngineered.Application;

public sealed record PlaceOrderResult(
    string OrderNumber,
    string CustomerId,
    decimal Total,
    string Status,
    string Summary);
