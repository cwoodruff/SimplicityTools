using Sample.Simplified.App.Fulfillment;
using Sample.Simplified.App.Orders;
using Xunit;

namespace Sample.Simplified.App.Tests.Fulfillment;

public sealed class FulfillmentServiceTests
{
    private static readonly IReadOnlyList<OrderLine> SampleLines =
    [
        new("BEANS-COLOMBIA", "Colombia Roast", Quantity: 2, UnitPrice: 18.50m)
    ];

    [Fact]
    public void Plan_UsesStandardPolicyForStandardRequests()
    {
        var service = new FulfillmentService([
            new StandardFulfillmentPolicy(),
            new ExpressFulfillmentPolicy()
        ]);

        var plan = service.Plan(DeliverySpeed.Standard, SampleLines);

        Assert.Equal("Ground", plan.Lane);
        Assert.Equal(4, plan.EstimatedDays);
        Assert.False(plan.RequiresSignature);
    }

    [Fact]
    public void Plan_UsesExpressPolicyForExpressRequests()
    {
        var service = new FulfillmentService([
            new StandardFulfillmentPolicy(),
            new ExpressFulfillmentPolicy()
        ]);

        var plan = service.Plan(DeliverySpeed.Express, SampleLines);

        Assert.Equal("Air", plan.Lane);
        Assert.Equal(1, plan.EstimatedDays);
        Assert.True(plan.RequiresSignature);
    }
}
