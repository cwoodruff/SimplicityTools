using Sample.Simplified.App.Catalog;
using Sample.Simplified.App.Fulfillment;
using Sample.Simplified.App.Payments;

namespace Sample.Simplified.App.Orders;

public sealed class OrderService
{
    private readonly ProductCatalog catalog;
    private readonly PaymentService paymentService;
    private readonly FulfillmentService fulfillmentService;

    public OrderService(ProductCatalog catalog, PaymentService paymentService, FulfillmentService fulfillmentService)
    {
        this.catalog = catalog;
        this.paymentService = paymentService;
        this.fulfillmentService = fulfillmentService;
    }

    public OrderReceipt PlaceOrder(PlaceOrderRequest request)
    {
        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Orders require at least one line.");
        }

        var lines = request.Lines.Select(CreateLine).ToArray();
        var total = lines.Sum(line => line.Subtotal);
        var payment = paymentService.Authorize(request.PaymentMethod, total);
        var shipment = fulfillmentService.Plan(request.RequestedSpeed, lines);

        return new OrderReceipt(request.CustomerName, lines.Length, total, shipment, payment);
    }

    private OrderLine CreateLine(OrderLineRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Line quantities must be positive.");
        }

        var product = catalog.GetBySku(request.Sku);
        return new OrderLine(product.Sku, product.Name, request.Quantity, product.UnitPrice);
    }
}
