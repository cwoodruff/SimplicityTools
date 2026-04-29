namespace Sample.OverEngineered.Application;

public interface IPlaceOrderCommandHandler
{
    PlaceOrderResult Handle(PlaceOrderCommand command);
}
