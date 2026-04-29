namespace Sample.OverEngineered.Presentation;

internal sealed record PlaceOrderRequest(
    string CustomerId,
    string ActorId,
    string Notes,
    IReadOnlyList<PlaceOrderLineRequest> Lines);
