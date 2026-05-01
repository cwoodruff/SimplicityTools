using Sample.Simplified.App.Catalog;
using Sample.Simplified.App.Fulfillment;
using Sample.Simplified.App.Orders;
using Sample.Simplified.App.Payments;

namespace Sample.Simplified.App.Composition;

public sealed class AppHost
{
    private AppHost(OrderService orders)
    {
        Orders = orders;
    }

    public OrderService Orders { get; }

    public static AppHost Create()
    {
        var catalog = new ProductCatalog();
        var paymentService = new PaymentService();
        var fulfillmentService = new FulfillmentService(
        [
            new StandardFulfillmentPolicy(),
            new ExpressFulfillmentPolicy()
        ]);

        return new AppHost(new OrderService(catalog, paymentService, fulfillmentService));
    }
}
