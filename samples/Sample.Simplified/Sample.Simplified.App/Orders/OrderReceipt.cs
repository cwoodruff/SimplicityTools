using Sample.Simplified.App.Fulfillment;
using Sample.Simplified.App.Payments;

namespace Sample.Simplified.App.Orders;

public sealed record OrderReceipt(
    string CustomerName,
    int LineCount,
    decimal Total,
    ShipmentPlan Shipment,
    PaymentAuthorization Payment)
{
    public string ToSummary() =>
        $"{CustomerName} placed {LineCount} line(s) totaling {Total:0.00}. Ship via {Shipment.Lane} in {Shipment.EstimatedDays} day(s). Approval {Payment.ApprovalCode}.";
}
