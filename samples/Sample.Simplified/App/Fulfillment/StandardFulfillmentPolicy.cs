using Sample.Simplified.App.Orders;

namespace Sample.Simplified.App.Fulfillment;

public sealed class StandardFulfillmentPolicy : IFulfillmentPolicy
{
    public bool CanHandle(DeliverySpeed requestedSpeed) => requestedSpeed == DeliverySpeed.Standard;

    public ShipmentPlan CreatePlan(IReadOnlyList<OrderLine> lines)
    {
        var requiresSignature = lines.Sum(line => line.Quantity) >= 6;
        return new ShipmentPlan(Lane: "Ground", EstimatedDays: 4, RequiresSignature: requiresSignature);
    }
}
