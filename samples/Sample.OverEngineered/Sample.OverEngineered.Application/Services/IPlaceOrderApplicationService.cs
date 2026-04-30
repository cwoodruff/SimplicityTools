namespace Sample.OverEngineered.Application;

public interface IPlaceOrderApplicationService
{
    PlaceOrderResult PlaceOrder(PlaceOrderCommand command);
}
