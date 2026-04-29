using Sample.OverEngineered.Domain.Contracts;
using Sample.OverEngineered.Domain.Entities;

namespace Sample.OverEngineered.WriteModel;

public interface IOrderWriteFacade
{
    Order CreateOrder(PlaceOrderInput input);
}
