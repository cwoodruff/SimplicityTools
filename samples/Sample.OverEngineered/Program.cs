using Sample.OverEngineered;
using Sample.OverEngineered.Presentation;

var endpoint = OverEngineeredCompositionRoot.Create().CreateOrderEndpoint();
var response = endpoint.Post(
    new PlaceOrderRequest(
        CustomerId: "customer-007",
        ActorId: "legacy-operator",
        Notes: "Need gift wrap, legacy shipping, and three approval hops.",
        Lines:
        [
            new PlaceOrderLineRequest("SKU-ALPHA", 2, 14.50m),
            new PlaceOrderLineRequest("SKU-BETA", 1, 89.00m),
        ]));

Console.WriteLine(response);
