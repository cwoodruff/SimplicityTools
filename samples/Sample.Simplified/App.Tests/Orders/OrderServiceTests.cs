using Sample.Simplified.App.Catalog;
using Sample.Simplified.App.Fulfillment;
using Sample.Simplified.App.Orders;
using Sample.Simplified.App.Payments;
using Xunit;

namespace Sample.Simplified.App.Tests.Orders;

public sealed class OrderServiceTests
{
    private static OrderService CreateService() =>
        new(
            new ProductCatalog(),
            new PaymentService(),
            new FulfillmentService([
                new StandardFulfillmentPolicy(),
                new ExpressFulfillmentPolicy()
            ]));

    [Fact]
    public void PlaceOrder_RejectsEmptyBaskets()
    {
        var service = CreateService();
        var request = new PlaceOrderRequest(
            CustomerName: "Empty Basket",
            RequestedSpeed: DeliverySpeed.Standard,
            PaymentMethod: PaymentMethod.Invoice,
            Lines: []);

        var exception = Assert.Throws<InvalidOperationException>(() => service.PlaceOrder(request));

        Assert.Equal("Orders require at least one line.", exception.Message);
    }

    [Fact]
    public void PlaceOrder_RejectsUnknownProducts()
    {
        var service = CreateService();
        var request = new PlaceOrderRequest(
            CustomerName: "Unknown Product",
            RequestedSpeed: DeliverySpeed.Standard,
            PaymentMethod: PaymentMethod.Invoice,
            Lines:
            [
                new OrderLineRequest("NOPE-404", Quantity: 1)
            ]);

        var exception = Assert.Throws<InvalidOperationException>(() => service.PlaceOrder(request));

        Assert.Equal("Unknown product 'NOPE-404'.", exception.Message);
    }
}
