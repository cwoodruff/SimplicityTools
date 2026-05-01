using Sample.Simplified.App.Composition;
using Sample.Simplified.App.Demo;
using Xunit;

namespace Sample.Simplified.Tests.EndToEnd;

public sealed class CheckoutWorkflowTests
{
    [Fact]
    public void DemoOrder_CompletesThroughThePrimaryPath()
    {
        var host = AppHost.Create();
        var receipt = host.Orders.PlaceOrder(DemoScenario.CreateWeekendOrder());

        Assert.Equal("Contoso Coffee", receipt.CustomerName);
        Assert.Equal(2, receipt.LineCount);
        Assert.Equal(45.75m, receipt.Total);
        Assert.Equal("Air", receipt.Shipment.Lane);
        Assert.Equal("CARD-0046", receipt.Payment.ApprovalCode);
    }
}
