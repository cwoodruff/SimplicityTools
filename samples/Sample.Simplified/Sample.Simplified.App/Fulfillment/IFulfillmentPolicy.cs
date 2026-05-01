using Sample.Simplified.App.Orders;

namespace Sample.Simplified.App.Fulfillment;

public interface IFulfillmentPolicy
{
    bool CanHandle(DeliverySpeed requestedSpeed);

    ShipmentPlan CreatePlan(IReadOnlyList<OrderLine> lines);
}
