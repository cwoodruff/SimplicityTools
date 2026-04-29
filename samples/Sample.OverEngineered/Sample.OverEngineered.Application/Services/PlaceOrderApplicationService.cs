using Sample.OverEngineered.Authorization;
using Sample.OverEngineered.Telemetry;
using Sample.OverEngineered.Validation;

namespace Sample.OverEngineered.Application;

public sealed class PlaceOrderApplicationService : IPlaceOrderApplicationService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IOrderValidator _orderValidator;
    private readonly ICommandBus _commandBus;
    private readonly ITelemetrySink _telemetrySink;

    public PlaceOrderApplicationService(
        IAuthorizationService authorizationService,
        IOrderValidator orderValidator,
        ICommandBus commandBus,
        ITelemetrySink telemetrySink)
    {
        _authorizationService = authorizationService;
        _orderValidator = orderValidator;
        _commandBus = commandBus;
        _telemetrySink = telemetrySink;
    }

    public PlaceOrderResult PlaceOrder(PlaceOrderCommand command)
    {
        _authorizationService.EnsureCanPlaceOrder(command.ActorId);
        _orderValidator.Validate(command.Input);
        var result = _commandBus.Send(command);
        _telemetrySink.Track("application.place-order.completed", result.OrderNumber);
        return result;
    }
}
