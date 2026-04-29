using Sample.OverEngineered.Domain.Entities;
using Sample.OverEngineered.ReadModel;

namespace Sample.OverEngineered.Application;

public sealed class PlaceOrderResultMapper : IPlaceOrderResultMapper
{
    public PlaceOrderResult Map(Order order, OrderReadModel? projection)
    {
        var status = projection?.Status ?? "Unknown";
        var summary = projection is null
            ? $"Order for {order.CustomerId} is queued without a projection."
            : $"Order for {projection.CustomerId} totals {projection.Total:C} and is {projection.Status}.";

        return new PlaceOrderResult(order.OrderNumber, order.CustomerId, order.Total.Amount, status, summary);
    }
}
