using Sample.Simplified.App.Fulfillment;
using Sample.Simplified.App.Orders;
using Sample.Simplified.App.Payments;

namespace Sample.Simplified.App.Demo;

public static class DemoScenario
{
    public static PlaceOrderRequest CreateWeekendOrder() =>
        new(
            CustomerName: "Contoso Coffee",
            RequestedSpeed: DeliverySpeed.Express,
            PaymentMethod: PaymentMethod.Card,
            Lines:
            [
                new OrderLineRequest("BEANS-COLOMBIA", Quantity: 2),
                new OrderLineRequest("FILTERS-PAPER", Quantity: 1)
            ]);
}
