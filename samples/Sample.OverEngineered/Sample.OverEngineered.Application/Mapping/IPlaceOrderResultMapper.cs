using Sample.OverEngineered.Domain.Entities;
using Sample.OverEngineered.ReadModel;

namespace Sample.OverEngineered.Application;

public interface IPlaceOrderResultMapper
{
    PlaceOrderResult Map(Order order, OrderReadModel? projection);
}
