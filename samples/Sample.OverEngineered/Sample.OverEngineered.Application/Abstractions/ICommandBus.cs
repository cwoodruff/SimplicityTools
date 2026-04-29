namespace Sample.OverEngineered.Application;

public interface ICommandBus
{
    PlaceOrderResult Send(PlaceOrderCommand command);
}
