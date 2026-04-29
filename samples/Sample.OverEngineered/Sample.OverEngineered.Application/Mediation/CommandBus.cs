namespace Sample.OverEngineered.Application;

public sealed class CommandBus : ICommandBus
{
    private readonly IPlaceOrderCommandHandler _placeOrderCommandHandler;

    public CommandBus(IPlaceOrderCommandHandler placeOrderCommandHandler)
    {
        _placeOrderCommandHandler = placeOrderCommandHandler;
    }

    public PlaceOrderResult Send(PlaceOrderCommand command) => _placeOrderCommandHandler.Handle(command);
}
