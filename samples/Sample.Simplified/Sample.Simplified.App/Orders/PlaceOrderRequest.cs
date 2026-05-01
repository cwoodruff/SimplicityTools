using Sample.Simplified.App.Fulfillment;
using Sample.Simplified.App.Payments;

namespace Sample.Simplified.App.Orders;

public sealed record PlaceOrderRequest(
    string CustomerName,
    DeliverySpeed RequestedSpeed,
    PaymentMethod PaymentMethod,
    IReadOnlyList<OrderLineRequest> Lines);
