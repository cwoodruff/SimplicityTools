using Sample.Simplified.App.Orders;

namespace Sample.Simplified.App.Fulfillment;

public sealed class FulfillmentService
{
    private readonly IReadOnlyList<IFulfillmentPolicy> policies;

    public FulfillmentService(IReadOnlyList<IFulfillmentPolicy> policies)
    {
        this.policies = policies;
    }

    public ShipmentPlan Plan(DeliverySpeed requestedSpeed, IReadOnlyList<OrderLine> lines)
    {
        var policy = policies.FirstOrDefault(candidate => candidate.CanHandle(requestedSpeed));
        if (policy is null)
        {
            throw new InvalidOperationException($"No fulfillment policy handles {requestedSpeed}.");
        }

        return policy.CreatePlan(lines);
    }
}
