using Sample.Simplified.App.Orders;

namespace Sample.Simplified.App.Fulfillment;

public sealed class ExpressFulfillmentPolicy : IFulfillmentPolicy
{
    public bool CanHandle(DeliverySpeed requestedSpeed) => requestedSpeed == DeliverySpeed.Express;

    public ShipmentPlan CreatePlan(IReadOnlyList<OrderLine> lines)
    {
        var requiresSignature = lines.Count > 0;
        return new ShipmentPlan(Lane: "Air", EstimatedDays: 1, RequiresSignature: requiresSignature);
    }
}
